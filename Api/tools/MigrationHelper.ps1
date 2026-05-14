<#
.SYNOPSIS
    一键迁移检测和生成工具 - 用于 ST 微服务项目

.DESCRIPTION
    提供迁移检测、批量生成、指定服务生成等功能

.PARAMETER Detect
    检测所有微服务是否有待生成的迁移（默认模式）

.PARAMETER Generate
    为所有或指定服务生成迁移

.PARAMETER Service
    指定要处理的服务，多个服务用逗号分隔
    示例：-Service Identity,OperationLog,FileUpload

.PARAMETER Message
    自定义迁移名称，若不指定则自动编号（0005、0006 等）

.PARAMETER Verbose
    显示详细的命令执行输出

.EXAMPLE
    # 检测所有服务
    .\MigrationHelper.ps1

    # 生成所有待迁移的服务
    .\MigrationHelper.ps1 -Generate

    # 生成指定服务的迁移
    .\MigrationHelper.ps1 -Generate -Service Identity,FileUpload

    # 生成迁移并指定名称
    .\MigrationHelper.ps1 -Generate -Service Identity -Message "AddUserAvatar"
#>

param(
    [switch]$Detect,
    [switch]$Generate,
    [string]$Service,
    [string]$Message,
    [switch]$Verbose
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ==================== 配置 ====================
$SolutionRoot = Split-Path -Parent $PSScriptRoot
$MicroservicesPath = Join-Path $SolutionRoot "src\Microservices"
$OperationLogConsumerPath = Join-Path $MicroservicesPath "OperationLog\ST.MS.OperationLog.Consumer"

# 定义所有有 Infra 的微服务
$AllServices = @{
    "Identity"     = @{ Infra = "ST.MS.Identity.Infra"; Api = "ST.MS.Identity.Api" }
    "OperationLog" = @{ Infra = "ST.MS.OperationLog.Infra"; Api = "ST.MS.OperationLog.Api" }
    "FileUpload"   = @{ Infra = "ST.MS.FileUpload.Infra"; Api = "ST.MS.FileUpload.Api" }
    "Test"         = @{ Infra = "ST.MS.Test.Infra"; Api = "ST.MS.Test.Api" }
}

# ==================== 日志和颜色 ====================
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✅ $Message" "Green"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠️  $Message" "Yellow"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "❌ $Message" "Red"
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "ℹ️  $Message" "Cyan"
}

# ==================== 工具函数 ====================

function Get-ServiceList {
    param([string]$ServiceFilter)
    
    if ([string]::IsNullOrWhiteSpace($ServiceFilter)) {
        return $AllServices.Keys | Sort-Object
    }
    
    $requested = $ServiceFilter -split "," | ForEach-Object { $_.Trim() }
    $available = @()
    
    foreach ($svc in $requested) {
        if ($AllServices.ContainsKey($svc)) {
            $available += $svc
        } else {
            Write-Warning "Service not found: $svc"
        }
    }
    
    return $available
}

function Get-InfraProjectPath {
    param([string]$ServiceName)
    
    $infraName = $AllServices[$ServiceName].Infra
    $path = Join-Path $MicroservicesPath $ServiceName $infraName
    return $path
}

function Get-ApiProjectPath {
    param([string]$ServiceName)
    
    $apiName = $AllServices[$ServiceName].Api
    $path = Join-Path $MicroservicesPath $ServiceName $apiName
    return $path
}

function Test-ServiceExists {
    param([string]$ServiceName)
    
    $infraPath = Get-InfraProjectPath $ServiceName
    $csprojPath = Join-Path $infraPath "$($AllServices[$ServiceName].Infra).csproj"
    
    return (Test-Path $csprojPath)
}

function Get-LatestMigrationNumber {
    param([string]$ServiceName)
    
    $infraPath = Get-InfraProjectPath $ServiceName
    $migrationsPath = Join-Path $infraPath "Migrations"
    
    if (-not (Test-Path $migrationsPath)) {
        return 0
    }
    
    $migrations = @(Get-ChildItem -Path $migrationsPath -Filter "*.cs" | 
        Where-Object { $_.Name -notmatch "ModelSnapshot" -and $_.Name -notmatch "Designer" })
    
    if ($migrations.Count -eq 0) {
        return 0
    }
    
    $latest = $migrations | Sort-Object Name | Select-Object -Last 1
    # 迁移格式: yyyyMMddHHmmss_<number>_<name>.cs 或 yyyyMMddHHmmss_<name>.cs
    # 需要提取下划线后面的数字部分
    $baseName = $latest.BaseName
    
    # 分割时间戳和后面的部分
    if ($baseName -match "^\d{14}_(\d+)_") {
        # 格式：timestamp_0001_name
        $number = [int]($baseName -replace "^\d{14}_(\d+)_.*$", "`$1")
        return $number
    }
    elseif ($baseName -match "^\d{14}_(\d+)$") {
        # 格式：timestamp_0001 (无名称)
        $number = [int]($baseName -replace "^\d{14}_(\d+)$", "`$1")
        return $number
    }
    elseif ($baseName -match "^\d{14}_[A-Za-z]") {
        # 格式：timestamp_name (初始迁移)
        return 0
    }
    
    return 0
}

function Check-PendingMigrations {
    param([string]$ServiceName)
    
    if (-not (Test-ServiceExists $ServiceName)) {
        return @{ Status = "NotFound"; Message = "Service not found" }
    }
    
    $infraPath = Get-InfraProjectPath $ServiceName
    $infraName = $AllServices[$ServiceName].Infra
    $apiPath = Get-ApiProjectPath $ServiceName
    $csprojPath = Join-Path $infraPath "$infraName.csproj"
    
    try {
        # 首先尝试通过 EF CLI 检查待迁移
        Write-Verbose "Checking pending migrations for $ServiceName..."
        Write-Verbose "  Infra path: $infraPath"
        Write-Verbose "  API path: $apiPath"
        
        # 需要在项目目录中执行命令
        $output = & dotnet ef migrations has-pending-model-changes `
            --project $infraPath `
            --startup-project $apiPath `
            2>&1
        
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -eq 0) {
            # Exit code 0 = no pending migrations
            return @{ 
                Status = "UpToDate"
                Message = "No pending migrations"
                LatestMigration = Get-LatestMigrationNumber $ServiceName
            }
        } else {
            # Exit code 1 = has pending migrations
            return @{
                Status = "Pending"
                Message = "Pending migrations detected"
                LatestMigration = Get-LatestMigrationNumber $ServiceName
            }
        }
    }
    catch {
        Write-Verbose "EF CLI check failed for ${ServiceName}: $_"
        
        # 备选方案：检查是否有 Migrations 文件夹和迁移文件
        $migrationsPath = Join-Path $infraPath "Migrations"
        if (-not (Test-Path $migrationsPath)) {
            # 从未生成过迁移
            return @{
                Status = "Pending"
                Message = "No migrations folder found (first migration)"
                LatestMigration = 0
            }
        }
        
        # 若已有迁移文件，优先采信 EF CLI 的判断
        # 如果 EF CLI 执行失败，说明环境配置有问题，标记为错误
        return @{
            Status = "Error"
            Message = "Failed to check migrations: $_"
            LatestMigration = Get-LatestMigrationNumber $ServiceName
        }
    }
}

function Generate-Migration {
    param(
        [string]$ServiceName,
        [string]$MigrationName
    )
    
    if (-not (Test-ServiceExists $ServiceName)) {
        Write-Error "Service not found: $ServiceName"
        return $false
    }
    
    $infraPath = Get-InfraProjectPath $ServiceName
    $infraName = $AllServices[$ServiceName].Infra
    $apiPath = Get-ApiProjectPath $ServiceName
    
    # 生成迁移名称
    if ([string]::IsNullOrWhiteSpace($MigrationName)) {
        $nextNumber = (Get-LatestMigrationNumber $ServiceName) + 1
        $paddedNumber = $nextNumber.ToString("0000")
        $MigrationName = $paddedNumber
    }
    
    try {
        Write-Info "Generating migration for ${ServiceName} with name: $MigrationName"
        
        $cmd = @(
            "dotnet", "ef", "migrations", "add", $MigrationName,
            "-p", $infraPath,
            "-s", $apiPath,
            "-o", "Migrations"
        )
        
        if ($Verbose) {
            Write-Verbose "Command: $($cmd -join ' ')"
        }
        
        & $cmd[0] $cmd[1..($cmd.Length - 1)]
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Migration generated for $ServiceName"
            return $true
        } else {
            Write-Error "Failed to generate migration for $ServiceName"
            return $false
        }
    }
    catch {
        Write-Error "Error generating migration for ${ServiceName}: $_"
        return $false
    }
}

# ==================== 主逻辑 ====================

function Main {
    Write-Info "ST Migration Helper - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    Write-Info "Solution Root: $SolutionRoot"
    Write-Host ""
    
    # 默认为检测模式
    if (-not $Generate -and -not $Detect) {
        $Detect = $true
    }
    
    $servicesToProcess = @(Get-ServiceList $Service)
    
    if ($servicesToProcess.Count -eq 0) {
        Write-Warning "No services found to process"
        exit 1
    }
    
    if ($Detect) {
        Invoke-DetectMode $servicesToProcess
    }
    elseif ($Generate) {
        Invoke-GenerateMode $servicesToProcess
    }
}

function Invoke-DetectMode {
    param([string[]]$Services)
    
    Write-ColorOutput "═══ MIGRATION DETECTION ═══" "Magenta"
    Write-Host ""
    
    $results = @()
    $pendingServices = @()
    
    foreach ($svc in $Services) {
        $status = Check-PendingMigrations $svc
        $results += @{
            Service = $svc
            Status = $status.Status
            Message = $status.Message
            Latest = $status.LatestMigration
        }
        
        if ($status.Status -eq "Pending") {
            $pendingServices += $svc
        }
    }
    
    # 输出结果表格
    Write-Host ""
    Write-Host "Service Status Report:"
    Write-Host ""
    
    foreach ($result in $results) {
        $displayStatus = switch ($result.Status) {
            "UpToDate" { "✅ Up-to-date" }
            "Pending" { "⚠️  Pending" }
            "NotFound" { "❌ Not Found" }
            "Error" { "❌ Error" }
            default { "❓ Unknown" }
        }
        
        $latestDisplay = if ($result.Latest -gt 0) { "#$($result.Latest)" } else { "None" }
        
        Write-Host ("{0,-15} {1,-20} (Latest: {2})" -f $result.Service, $displayStatus, $latestDisplay)
    }
    
    Write-Host ""
    
    if ($pendingServices.Count -gt 0) {
        Write-ColorOutput "📊 Summary: $($pendingServices.Count) service(s) have pending migrations:" "Yellow"
        Write-Host ($pendingServices -join ", ")
        Write-Host ""
        Write-Info "Run with -Generate flag to generate migrations:"
        Write-Host "  .\MigrationHelper.ps1 -Generate"
        Write-Host "  .\MigrationHelper.ps1 -Generate -Service $($pendingServices[0])"
    } else {
        Write-Success "All services are up-to-date!"
    }
    
    Write-Host ""
}

function Invoke-GenerateMode {
    param([string[]]$Services)
    
    Write-ColorOutput "═══ MIGRATION GENERATION ═══" "Magenta"
    Write-Host ""
    
    $successCount = 0
    $failureCount = 0
    
    foreach ($svc in $Services) {
        $status = Check-PendingMigrations $svc
        
        if ($status.Status -eq "Error") {
            Write-Error "Skipping ${svc} due to detection error: $($status.Message)"
            $failureCount++
            continue
        }
        
        if ($status.Status -eq "NotFound") {
            Write-Warning "Skipping ${svc}: Service not found"
            $failureCount++
            continue
        }
        
        if ($status.Status -eq "UpToDate") {
            Write-Info "Skipping ${svc}: No pending migrations"
            continue
        }
        
        # 生成迁移
        if (Generate-Migration $svc $Message) {
            $successCount++
        } else {
            $failureCount++
        }
    }
    
    Write-Host ""
    Write-ColorOutput "═══ GENERATION SUMMARY ═══" "Magenta"
    Write-Success "Successfully generated: $successCount"
    if ($failureCount -gt 0) {
        Write-Error "Failed: $failureCount"
    }
    
    if ($failureCount -gt 0) {
        exit 1
    }
}

# 执行主逻辑
try {
    Main
}
catch {
    Write-Error "Fatal error: $_"
    exit 1
}
