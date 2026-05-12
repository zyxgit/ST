<#
.SYNOPSIS
  ST 微服务迁移管理工具 — 一键检查/新增/移除迁移
.DESCRIPTION
  统一管理所有微服务的 EF Core 迁移，免去手动拼路径。
  用法:
    migrate.ps1 check  [service]                    检查模型变更
    migrate.ps1 list   [service]                    列出迁移
    migrate.ps1 add    <service> [name]             新增迁移（name 省略时自动编号 000n）
    migrate.ps1 remove <service>                    移除最后一条迁移
    migrate.ps1 update <service> [migration]        更新数据库
    migrate.ps1 script <service> [from] [to]        生成 SQL 脚本
    migrate.ps1 help                                显示帮助
  选项:
    --no-build   跳过构建步骤（除 check 外均支持）
#>

param(
  [Parameter(Position = 0)]
  [string]$Command = 'help',

  [Parameter(Position = 1)]
  [string]$ServiceName = '',

  [Parameter(Position = 2)]
  [string]$MigrationName = '',

  [Parameter(Position = 3)]
  [string]$ScriptTo = '',

  [Alias('no-build')]
  [switch]$NoBuild
)

# ── 初始化 ──────────────────────────────────────────────────────────────
$scriptPath = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$repoRoot   = Resolve-Path "$scriptPath/../.."
$toolsDir   = Resolve-Path $scriptPath
$configPath = Join-Path $toolsDir 'migrations.json'

if (-not (Test-Path $configPath)) { Write-Host "✗ 配置文件不存在: $configPath" -Foreground Red; exit 1 }
$config = Get-Content $configPath -Raw | ConvertFrom-Json

$ScriptName = Split-Path -Leaf $PSCommandPath

# ── 颜色 ────────────────────────────────────────────────────────────────
$C = @{ G = 'Green'; Y = 'Yellow'; R = 'Red'; C = 'Cyan'; D = 'DarkGray'; M = 'Magenta' }

function Write-Title  { Write-Host @args -Foreground $C.C }
function Write-Ok     { Write-Host @args -Foreground $C.G }
function Write-Warn   { Write-Host @args -Foreground $C.Y }
function Write-Err    { Write-Host @args -Foreground $C.R }
function Write-Dim    { Write-Host @args -Foreground $C.D }

# ── 辅助 ────────────────────────────────────────────────────────────────
function Test-DotnetEf {
  try { $null = Invoke-DotnetEf @('--version') 2>&1 | Out-Null; return $true }
  catch { return $false }
}

# 在 Api/src/ 下运行 dotnet ef，确保使用本地工具清单中的版本
function Invoke-DotnetEf {
  param([string[]]$Arguments)
  Push-Location "$repoRoot/Api/src"
  try { dotnet ef @Arguments } finally { Pop-Location }
}

function Get-Svc($Name) {
  if (-not $Name -or -not $config.services.$Name) { return $null }
  $s = $config.services.$Name
  return @{
    Name      = $Name
    DbContext = $s.dbContext
    Infra     = $s.infra
    Api       = $s.api
    Database  = $s.database
    InfraPath = Join-Path $repoRoot ($s.infra -replace '/', '\')
    ApiPath   = Join-Path $repoRoot ($s.api   -replace '/', '\')
  }
}

function Resolve-Svc($id) {
  if ($id -match '^\d+$') {
    $num = [int]$id; $i = 0
    foreach ($prop in $config.services.PSObject.Properties) {
      $i++
      if ($i -eq $num) { return Get-Svc $prop.Name }
    }
    return $null
  }
  return Get-Svc $id
}

function Get-SvcList($Name) {
  if ($Name) {
    $s = Resolve-Svc $Name
    if (-not $s) { Write-Err "✗ 未知服务: $Name"; exit 1 }
    return @($s)
  }
  return $config.services.PSObject.Properties | ForEach-Object { Get-Svc $_.Name }
}

function Get-EfArgs($s, [switch]$NoBuild) {
  $a = @(
    '--project',         $s.InfraPath
    '--startup-project', $s.ApiPath
    '--context',         $s.DbContext
  )
  if ($NoBuild) { $a += '--no-build' }
  return $a
}

# ── 命令: help ─────────────────────────────────────────────────────────
function Show-Help {
  Write-Title 'ST 微服务迁移管理工具'
  Write-Host
  Write-Host '用法:'
  Write-Host "  $ScriptName check  [service]"                     -Foreground $C.G
  Write-Host "  $ScriptName list   [service]"                     -Foreground $C.G
  Write-Host "  $ScriptName add    <service> [name]"              -Foreground $C.G
  Write-Host "  $ScriptName remove <service>"                     -Foreground $C.G
  Write-Host "  $ScriptName update <service> [migration]"         -Foreground $C.G
  Write-Host "  $ScriptName script <service> [from] [to]"         -Foreground $C.G
  Write-Host "  $ScriptName help"                                 -Foreground $C.G
  Write-Host
  Write-Host '服务列表:' -Foreground $C.C
  $i = 0
  $config.services.PSObject.Properties | ForEach-Object {
    $i++
    $s = $_.Value
    Write-Host "  [$i] $($_.Name)".PadRight(20) -NoNewline
    Write-Dim " → $($s.database)  ($($s.dbContext))"
  }
}

# ── 命令: check ─────────────────────────────────────────────────────────
function Invoke-Check {
  $svcs = Get-SvcList $ServiceName
  $anyPending = $false

  foreach ($s in $svcs) {
    $label = "[$($s.Name)]".PadRight(16)

    $output = Invoke-DotnetEf -Arguments (@('migrations', 'has-pending-model-changes') + $(Get-EfArgs $s)) 2>&1 | Out-String -Width 4096

    if ($LASTEXITCODE -eq 0) {
      Write-Ok "  $label ✓ 无需迁移"
    } elseif ($LASTEXITCODE -eq 1) {
      Write-Warn "  $label ⚠ 有模型变更未迁移"
      Write-Dim "           → 执行: dotnet ef migrations add <名称> $(Get-EfArgs $s)"
      $anyPending = $true
    } else {
      Write-Err "  $label ✗ 检查失败"
      Write-Host "    $($output.Trim())" -Foreground $C.D
    }
  }

  if ($anyPending) { exit 1 }
}

# ── 命令: list ──────────────────────────────────────────────────────────
function Invoke-List {
  $svcs = Get-SvcList $ServiceName
  foreach ($s in $svcs) {
    $label = "[$($s.Name)]".PadRight(16)
    Write-Title "  $label 迁移列表:"

    $output = Invoke-DotnetEf -Arguments (@('migrations', 'list') + $(Get-EfArgs $s)) 2>&1
    if ($LASTEXITCODE -ne 0) {
      Write-Err "    $output"
    } else {
      $output -split "`n" | Where-Object { $_ -match '\d{14}' } | ForEach-Object {
        $parts = $_ -split '\s+', 2
        $status = if ($parts[0] -match 'Pending') { '⏳' } else { '✓' }
        Write-Host "    $status $($parts[-1])"
      }
    }
    Write-Host
  }
}

# ── 命令: add ───────────────────────────────────────────────────────────
function Invoke-Add {
  if (-not $ServiceName) {
    Write-Err '✗ 用法: migrate.ps1 add <service> [name]'; exit 1
  }
  $s = Resolve-Svc $ServiceName
  if (-not $s) { Write-Err "✗ 未知服务: $ServiceName"; exit 1 }

  # name 省略时自动编号 000n
  if (-not $MigrationName) {
    $existing = Get-ChildItem "$($s.InfraPath)/Migrations/*.cs" -ErrorAction SilentlyContinue |
      Where-Object { $_.Name -match '_(\d{4})\.cs$' } |
      ForEach-Object { [int]$Matches[1] }
    $nextNum = if ($existing) { ($existing | Sort-Object -Descending | Select-Object -First 1) + 1 } else { 1 }
    $MigrationName = '{0:D4}' -f $nextNum
    Write-Dim "  → 未指定名称，自动编号: $MigrationName"
  }

  Write-Dim "  → 生成迁移 $MigrationName ..."
  $output = Invoke-DotnetEf -Arguments (@('migrations', 'add', $MigrationName) + $(Get-EfArgs $s -NoBuild:$NoBuild)) 2>&1
  if ($LASTEXITCODE -eq 0) {
    Write-Ok "  ✓ 已生成迁移: $MigrationName"
    $output -split "`n" | Where-Object { $_ -match '\.cs\b' -and $_ -notmatch 'Designer' -and $_ -notmatch 'Snapshot' } | ForEach-Object {
      Write-Dim "    $_"
    }
  } else {
    Write-Err "  ✗ 迁移生成失败:"
    Write-Host "    $output"
    exit $LASTEXITCODE
  }
}

# ── 命令: remove ────────────────────────────────────────────────────────
function Invoke-Remove {
  if (-not $ServiceName) {
    Write-Err '✗ 用法: migrate.ps1 remove <service>'; exit 1
  }
  $s = Resolve-Svc $ServiceName
  if (-not $s) { Write-Err "✗ 未知服务: $ServiceName"; exit 1 }

  Write-Host "  即将移除 $($s.Name) 的最后一条迁移，是否继续? [y/N] " -Foreground $C.Y -NoNewline
  $confirm = Read-Host
  if ($confirm -notmatch '^[yY]') { Write-Dim '  已取消'; return }

  $output = Invoke-DotnetEf -Arguments (@('migrations', 'remove') + $(Get-EfArgs $s -NoBuild:$NoBuild)) 2>&1
  if ($LASTEXITCODE -eq 0) {
    Write-Ok '  ✓ 已移除最后一条迁移'
  } else {
    Write-Err "  ✗ 移除失败:"
    Write-Host "    $output"
    exit $LASTEXITCODE
  }
}

# ── 命令: update ────────────────────────────────────────────────────────
function Invoke-Update {
  if (-not $ServiceName) {
    Write-Err '✗ 用法: migrate.ps1 update <service> [migration]'; exit 1
  }
  $s = Resolve-Svc $ServiceName
  if (-not $s) { Write-Err "✗ 未知服务: $ServiceName"; exit 1 }

  Write-Dim "  → 更新数据库 $($s.Database) ..."
  $args = @('database', 'update')
  if ($MigrationName) { $args += $MigrationName }
  $args += Get-EfArgs $s -NoBuild:$NoBuild
  $output = Invoke-DotnetEf -Arguments $args 2>&1
  if ($LASTEXITCODE -eq 0) {
    Write-Ok '  ✓ 数据库已更新'
  } else {
    Write-Err "  ✗ 数据库更新失败:"
    Write-Host "    $output"
    exit $LASTEXITCODE
  }
}

# ── 命令: script ────────────────────────────────────────────────────────
function Invoke-Script {
  if (-not $ServiceName) {
    Write-Err '✗ 用法: migrate.ps1 script <service> [from] [to]'; exit 1
  }
  $s = Resolve-Svc $ServiceName
  if (-not $s) { Write-Err "✗ 未知服务: $ServiceName"; exit 1 }

  $scriptDir = Join-Path $repoRoot 'scripts'
  New-Item -ItemType Directory -Path $scriptDir -Force | Out-Null
  $outputFile = Join-Path $scriptDir "$($s.Name)_$(Get-Date -Format 'yyyyMMddHHmmss').sql"

  Write-Dim "  → 生成 SQL 脚本 ..."
  $args = @('migrations', 'script', '--output', $outputFile)
  if ($MigrationName) { $args += '--from', $MigrationName }
  if ($ScriptTo) { $args += '--to', $ScriptTo }
  if ($NoBuild) { $args += '--no-build' }
  $args += Get-EfArgs $s -NoBuild:$true
  $output = Invoke-DotnetEf -Arguments $args 2>&1
  if ($LASTEXITCODE -eq 0) {
    Write-Ok "  ✓ SQL 脚本已生成: $outputFile"
  } else {
    Write-Err "  ✗ SQL 脚本生成失败:"
    Write-Host "    $output"
    exit $LASTEXITCODE
  }
}

# ── 入口 ────────────────────────────────────────────────────────────────
try {
  if (-not (Test-DotnetEf)) {
    Write-Err '✗ 未安装 dotnet-ef 工具。请执行:'
    Write-Host "    dotnet tool install --global dotnet-ef" -Foreground $C.Y
    exit 1
  }

  switch ($Command.ToLower()) {
    'check'  { Invoke-Check }
    'list'   { Invoke-List }
    'add'    { Invoke-Add }
    'remove' { Invoke-Remove }
    'update' { Invoke-Update }
    'script' { Invoke-Script }
    default  { Show-Help }
  }
} finally {
  # 双击运行时暂停，避免窗口一闪而过
  # finally 会在 exit 之前执行，确保用户能看到输出
  $needPause = $false
  if ($host.Name -eq 'ConsoleHost' -and -not $env:CI) {
    try { $needPause = -not [Console]::IsOutputRedirected } catch { $needPause = $true }
  }
  if ($needPause) {
    Write-Host
    Read-Host '按 Enter 键退出...'
  }
}
