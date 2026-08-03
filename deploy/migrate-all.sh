#!/bin/bash

# 读取 .env 文件
ENV_FILE=".env"
if [ ! -f "$ENV_FILE" ]; then
  echo "❌ 未找到 deploy/.env，请先复制 .env.example 为 .env 并填写配置"
  exit 1
fi

# 加载环境变量（跳过注释和空行）
set -a
source <(grep -v '^\s*#' "$ENV_FILE" | grep -v '^\s*$')
set +a

CONN_PREFIX="Host=127.0.0.1;Port=${POSTGRES_HOST_PORT:-25432};Username=${PGUSER};Password=${PGPASSWORD}"

declare -A SERVICES=(
  ["st_identity"]="Identity"
  ["st_operationlog"]="OperationLog"
  ["st_fileupload"]="FileUpload"
  ["st_order"]="Order"
  ["st_inventory"]="Inventory"
  ["st_payment"]="Payment"
)

for db in "${!SERVICES[@]}"; do
  svc="${SERVICES[$db]}"
  echo ">>> Updating $db ..."
  export Database__ConnectionString="${CONN_PREFIX};Database=$db"
  dotnet ef database update \
    --project "Api/src/Microservices/$svc/ST.MS.$svc.Infra" \
    --startup-project "Api/src/Microservices/$svc/ST.MS.$svc.Api" \
    --configuration Release
done
