# =============================================================================
# ST 微服务 — Production 部署脚本
# 用法：
#   首次部署/升级数据库： .\deploy-production.ps1 -Tag sha-abc1234 -MigrateOnly
#   启动/更新服务：       .\deploy-production.ps1 -Tag sha-abc1234
#   仅启动基础设施：      .\deploy-production.ps1 -Tag sha-abc1234 -InfraOnly
#   完整部署：            先执行 -MigrateOnly，再执行不带参数的版本
# 注意：生产环境必须指定固定 IMAGE_TAG，禁止使用 latest
# =============================================================================
param(
    [Parameter(Mandatory = $true)]
    [string]$Tag,

    [Parameter(Mandatory = $false)]
    [switch]$MigrateOnly,

    [Parameter(Mandatory = $false)]
    [switch]$InfraOnly
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $PSCommandPath

if ($Tag -eq "latest") {
    Write-Error "生产环境禁止使用 latest tag，请指定具体版本号（如 sha-abc1234）"
    exit 1
}

Write-Host "=== ST Production 部署 ===" -ForegroundColor Cyan
Write-Host "镜像 tag: $Tag" -ForegroundColor Gray

# 写入 IMAGE_TAG 到 .env.production
$envFile = Join-Path $ScriptDir ".env.production"
if (-not (Test-Path $envFile)) {
    Write-Error "未找到 $envFile，请先创建"
    exit 1
}
(Get-Content $envFile) -replace '^IMAGE_TAG=.*', "IMAGE_TAG=$Tag" | Set-Content $envFile

if ($InfraOnly) {
    Write-Host "`n>>> 启动基础设施..." -ForegroundColor Yellow
    docker compose --env-file "$ScriptDir/.env.production" -f "$ScriptDir/docker-compose.production.yml" up -d postgres cache rabbitmq
    if ($LASTEXITCODE -ne 0) { throw "Infra up failed" }
    Write-Host "`n=== 基础设施已启动 ===" -ForegroundColor Cyan
    exit 0
}

# 拉取镜像
Write-Host "`n>>> 拉取镜像..." -ForegroundColor Yellow
docker compose --env-file "$ScriptDir/.env.production" -f "$ScriptDir/docker-compose.production.yml" pull
if ($LASTEXITCODE -ne 0) { throw "Pull failed" }

if ($MigrateOnly) {
    Write-Host "`n>>> 执行数据库迁移..." -ForegroundColor Yellow
    Write-Host "启动迁移服务（等待迁移完成后会自动退出）..." -ForegroundColor Gray
    docker compose --env-file "$ScriptDir/.env.production" -f "$ScriptDir/docker-compose.production.yml" --profile migrate up --abort-on-container-exit
    if ($LASTEXITCODE -ne 0) { throw "Migration failed" }
    Write-Host "`n=== 数据库迁移完成 ===" -ForegroundColor Cyan
    Write-Host "现在可以执行不带 -MigrateOnly 参数部署业务服务" -ForegroundColor Gray
    exit 0
}

# 启动业务服务
Write-Host "`n>>> 启动业务服务..." -ForegroundColor Yellow
docker compose --env-file "$ScriptDir/.env.production" -f "$ScriptDir/docker-compose.production.yml" up -d st-gateway st-ms-identity-api st-ms-test-api st-ms-fileupload-api st-ms-operationlog-api st-ms-operationlog-consumer
if ($LASTEXITCODE -ne 0) { throw "Up failed" }

Write-Host "`n=== 部署完成 ===" -ForegroundColor Cyan
Write-Host "Gateway: http://<host-ip>:8080" -ForegroundColor Green
