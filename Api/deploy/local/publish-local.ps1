# =============================================================================
# ST 微服务 — 本地镜像构建脚本
# 将每个微服务构建为 Docker 镜像（tag: st/<name>:local），
# 用于在 docker-compose 中替换 GHCR 镜像进行本地测试。
# 用法： .\publish-local.ps1
# 然后手动将 .env.local 中的 IMAGE_TAG=latest 改为 IMAGE_TAG=local
# =============================================================================
$ErrorActionPreference = "Stop"

# 仓库根目录（脚本所在目录的上级）
$RepoRoot = Resolve-Path "$PSScriptRoot/../.."
$SolutionDir = "$RepoRoot/Api/src"

# 输出临时目录
$OutputRoot = "$RepoRoot/.publish-local"

Write-Host "=== ST 微服务本地构建 ===" -ForegroundColor Cyan
Write-Host "仓库路径: $RepoRoot" -ForegroundColor Gray

# ============================================================
# 服务列表：name -> 项目文件相对路径（相对于 Api/src/）
# name 同时也是 Docker image tag 的一部分（st/<name>:local）
# ============================================================
$Services = @(
    @{ Name = "gateway";             Project = "Microservices/Gateway/ST.Gateway/ST.Gateway.csproj" }
    @{ Name = "identity-api";        Project = "Microservices/Identity/ST.MS.Identity.Api/ST.MS.Identity.Api.csproj" }
    @{ Name = "test-api";            Project = "Microservices/Test/ST.MS.Test.Api/ST.MS.Test.Api.csproj" }
    @{ Name = "fileupload-api";      Project = "Microservices/FileUpload/ST.MS.FileUpload.Api/ST.MS.FileUpload.Api.csproj" }
    @{ Name = "operationlog-api";    Project = "Microservices/OperationLog/ST.MS.OperationLog.Api/ST.MS.OperationLog.Api.csproj" }
    @{ Name = "operationlog-consumer"; Project = "Microservices/OperationLog/ST.MS.OperationLog.Consumer/ST.MS.OperationLog.Consumer.csproj" }
)

# 清除上次构建输出
if (Test-Path $OutputRoot) {
    Remove-Item -Recurse -Force $OutputRoot
}

# ============================================================
# Step 1: 全局 restore（只一次）
# ============================================================
Write-Host "`n>>> dotnet restore..." -ForegroundColor Yellow
dotnet restore "$SolutionDir/ST.slnx" --nologo
if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

# ============================================================
# Step 2: 逐个构建镜像
# ============================================================
foreach ($svc in $Services) {
    $name = $svc.Name
    $project = $svc.Project
    $projectFull = Join-Path $SolutionDir $project
    $outputDir = Join-Path $OutputRoot $name

    Write-Host "`n>>> [$name] 构建中..." -ForegroundColor Yellow

    # 2a. dotnet publish 输出文件
    dotnet publish $projectFull -c Release -o $outputDir --nologo
    if ($LASTEXITCODE -ne 0) { throw "[$name] publish failed" }

    # 2b. 从 .csproj 文件名推断入口 DLL
    $dllName = "$([System.IO.Path]::GetFileNameWithoutExtension($project)).dll"

    # 2c. 生成 Dockerfile
    @"
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "$dllName"]
"@ | Set-Content -Path "$outputDir/Dockerfile" -NoNewline

    # 2d. docker build（使用 local tag，避免覆盖 GHCR latest）
    docker build -t "st/${name}:local" $outputDir
    if ($LASTEXITCODE -ne 0) { throw "[$name] docker build failed" }

    Write-Host "  >> st/${name}:local 构建完成" -ForegroundColor Green
}

# 清理临时输出
Remove-Item -Recurse -Force $OutputRoot

Write-Host "`n=== 全部构建完成 ===" -ForegroundColor Cyan
Write-Host "请修改 .env.local 中 IMAGE_TAG=latest 为 IMAGE_TAG=local，然后启动：" -ForegroundColor Yellow
Write-Host "  docker compose --env-file .env.local -f docker-compose.local.yml up -d --pull never" -ForegroundColor Gray
