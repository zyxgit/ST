# =============================================================================
# ST 微服务 — Test 部署脚本
# 用法：
#   .\deploy-test.ps1                    # 部署 latest 版本
#   .\deploy-test.ps1 -Tag sha-abc1234   # 部署指定版本
# =============================================================================
param(
    [Parameter(Mandatory = $false)]
    [string]$Tag = "latest"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $PSCommandPath

Write-Host "=== ST Test 部署 ===" -ForegroundColor Cyan
Write-Host "镜像 tag: $Tag" -ForegroundColor Gray

# 写入 IMAGE_TAG 到 .env.test
$envFile = Join-Path $ScriptDir ".env.test"
if (-not (Test-Path $envFile)) {
    Write-Error "未找到 $envFile，请先创建"
    exit 1
}

# 更新 .env.test 中的 IMAGE_TAG
(Get-Content $envFile) -replace '^IMAGE_TAG=.*', "IMAGE_TAG=$Tag" | Set-Content $envFile

Write-Host "`n>>> 拉取最新镜像..." -ForegroundColor Yellow
docker compose --env-file "$ScriptDir/.env.test" -f "$ScriptDir/docker-compose.test.yml" pull
if ($LASTEXITCODE -ne 0) { throw "Pull failed" }

Write-Host "`n>>> 启动所有服务..." -ForegroundColor Yellow
docker compose --env-file "$ScriptDir/.env.test" -f "$ScriptDir/docker-compose.test.yml" up -d
if ($LASTEXITCODE -ne 0) { throw "Up failed" }

Write-Host "`n=== 部署完成 ===" -ForegroundColor Cyan
Write-Host "Gateway: http://localhost:8080" -ForegroundColor Green
Write-Host "RabbitMQ: http://localhost:15672 (guest/guest)" -ForegroundColor Green
