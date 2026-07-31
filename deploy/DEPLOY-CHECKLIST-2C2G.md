# ST 项目 2C2G 服务器部署清单

> Ubuntu 24.04 LTS · 2 vCPU · 2 GB RAM · 40 GB SSD

---

## 〇、服务器基础安全

```bash
# SSH 登录后第一件事
sudo apt update && sudo apt upgrade -y

# 创建新用户（可选，避免用 root）
adduser deploy
usermod -aG docker deploy
usermod -aG sudo deploy

# 配置 SSH 密钥登录，禁止密码登录（可选但推荐）
# vim /etc/ssh/sshd_config → PasswordAuthentication no
# sudo systemctl restart sshd
```

---

## 一、系统配置

### 1.1 加 2G Swap（必做）

```bash
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab

# 验证
free -h   # 应看到 Swap: 2.0Gi
```

### 1.2 设置时区（可选）

```bash
sudo timedatectl set-timezone Asia/Shanghai
```

---

## 二、安装 Docker

```bash
# 安装 Docker + Docker Compose v2
curl -fsSL https://get.docker.com | sudo sh

# 将当前用户加入 docker 组（免 sudo）
sudo usermod -aG docker $USER

# 重新登录使组生效，或执行：
newgrp docker

# 验证
docker --version          # Docker version 27.x+
docker compose version    # Docker Compose version v2.x+
```

---

## 三、拉取代码

```bash
# 选择一个目录
cd ~
git clone https://github.com/zyxgit/st.git
cd st/deploy
```

---

## 四、配置环境变量

```bash
cp .env.2c2g.example .env
vim .env
```

**必须修改的项：**

```env
# 数据库密码（改成强密码）
PGPASSWORD=YourStr0ngPGPass!

# Redis 密码
REDIS_PASSWORD=YourStr0ngRedisPass!

# RabbitMQ 密码
RABBITPASSWORD=YourStr0ngRabbitPass!

# JWT 签名密钥（base64 编码，至少 32 字节）
# 生成方法: openssl rand -base64 32
JWTSIGNINGKEY=这里粘贴生成的密钥
```

**端口映射保持默认即可**，如需修改注意不要冲突：

```env
POSTGRES_HOST_PORT=25432
REDIS_HOST_PORT=26379
RABBITMQ_AMQP_HOST_PORT=25672
IDENTITY_HOST_PORT=27127
OPERATIONLOG_HOST_PORT=21001
FILEUPLOAD_HOST_PORT=27250
ORDER_HOST_PORT=25090
INVENTORY_HOST_PORT=25091
PAYMENT_HOST_PORT=25092
GATEWAY_HOST_PORT=25000
WEB_HOST_PORT=28080
```

---

## 五、启动基础设施

> 先只启动 PostgreSQL、Redis、RabbitMQ，确认正常后再启动应用。

```bash
cd ~/st/deploy

docker compose -f docker-compose.2c2g.yml --env-file .env up -d postgres redis rabbitmq
```

### 5.1 检查基础设施状态

```bash
docker compose -f docker-compose.2c2g.yml ps

# 应看到三个容器都是 healthy:
# postgres   ... Up (healthy)
# redis      ... Up (healthy)
# rabbitmq   ... Up (healthy)
```

### 5.2 测试连接

```bash
# 测试 PostgreSQL
docker compose -f docker-compose.2c2g.yml exec postgres psql -U $PGUSER -c "SELECT 1;"

# 测试 Redis
docker compose -f docker-compose.2c2g.yml exec redis redis-cli -a $REDIS_PASSWORD ping

# 测试 RabbitMQ
docker compose -f docker-compose.2c2g.yml exec rabbitmq rabbitmqctl status
```

---

## 六、安装 .NET SDK 并执行数据库迁移

> 迁移需要 .NET SDK + dotnet-ef 工具，CI 也是这么做的。

### 6.1 安装 .NET 10 SDK

```bash
# 添加 Microsoft 包源
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# 安装 SDK
sudo apt update
sudo apt install -y dotnet-sdk-10.0

# 验证
dotnet --version
```

### 6.2 安装 EF Core 工具

```bash
dotnet tool install --global dotnet-ef
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc

# 验证
dotnet ef --version
```

### 6.3 执行迁移

> 每个微服务有独立的数据库，需要分别执行迁移。

```bash
cd ~/st

# 加载 .env 环境变量
set -a && source ~/st/deploy/.env && set +a

# 强制使用 Production 环境，避免读取 appsettings.Development.json
export ASPNETCORE_ENVIRONMENT=Production

# 还原 NuGet 包
dotnet restore Api/src/ST.slnx

# 构造连接字符串前缀
CONN_PREFIX="Host=127.0.0.1;Port=25432;Username=${PGUSER};Password=${PGPASSWORD}"

# ── Identity 数据库 ──
export Database__ConnectionString="${CONN_PREFIX};Database=st_identity"
dotnet ef database update \
  --project Api/src/Microservices/Identity/ST.MS.Identity.Infra \
  --startup-project Api/src/Microservices/Identity/ST.MS.Identity.Api \
  --configuration Release

# ── OperationLog 数据库 ──
export Database__ConnectionString="${CONN_PREFIX};Database=st_operationlog"
dotnet ef database update \
  --project Api/src/Microservices/OperationLog/ST.MS.OperationLog.Infra \
  --startup-project Api/src/Microservices/OperationLog/ST.MS.OperationLog.Api \
  --configuration Release

# ── FileUpload 数据库 ──
export Database__ConnectionString="${CONN_PREFIX};Database=st_fileupload"
dotnet ef database update \
  --project Api/src/Microservices/FileUpload/ST.MS.FileUpload.Infra \
  --startup-project Api/src/Microservices/FileUpload/ST.MS.FileUpload.Api \
  --configuration Release

# ── Order 数据库 ──
export Database__ConnectionString="${CONN_PREFIX};Database=st_order"
dotnet ef database update \
  --project Api/src/Microservices/Order/ST.MS.Order.Infra \
  --startup-project Api/src/Microservices/Order/ST.MS.Order.Api \
  --configuration Release

# ── Inventory 数据库 ──
export Database__ConnectionString="${CONN_PREFIX};Database=st_inventory"
dotnet ef database update \
  --project Api/src/Microservices/Inventory/ST.MS.Inventory.Infra \
  --startup-project Api/src/Microservices/Inventory/ST.MS.Inventory.Api \
  --configuration Release

# ── Payment 数据库 ──
export Database__ConnectionString="${CONN_PREFIX};Database=st_payment"
dotnet ef database update \
  --project Api/src/Microservices/Payment/ST.MS.Payment.Infra \
  --startup-project Api/src/Microservices/Payment/ST.MS.Payment.Api \
  --configuration Release

# 清除临时变量
unset Database__ConnectionString
```

### 6.4 验证数据库

```bash
docker compose -f docker-compose.2c2g.yml exec postgres \
  psql -U $PGUSER -c "\l" | grep st_

# 应看到:
# st_identity
# st_operationlog
# st_fileupload
# st_order
# st_inventory
# st_payment
```

---

## 七、拉取镜像并启动全部应用

> 镜像由 GitHub Actions 构建并推送至 ghcr.io，服务器只需拉取即可。

### 7.1 登录 ghcr.io（首次拉取需要）

```bash
# 使用 GitHub Personal Access Token 登录（需有 read:packages 权限）
echo "$GITHUB_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

### 7.2 启动所有服务

```bash
cd ~/st/deploy

# 拉取最新镜像并启动所有服务
docker compose -f docker-compose.2c2g.yml --env-file .env up -d --pull always

# 查看状态（等待所有容器 healthy）
docker compose -f docker-compose.2c2g.yml ps

# 实时查看资源占用
docker stats --no-stream
```

### 7.3 等待服务就绪

```bash
# 等待 Gateway 健康检查通过
for i in $(seq 1 30); do
  if curl -sf http://127.0.0.1:25000/health > /dev/null 2>&1; then
    echo "✅ Gateway is ready"
    break
  fi
  echo "⏳ Waiting... ($i/30)"
  sleep 2
done
```

---

## 八、开启数据种子

> 迁移完成后，开启种子数据初始化（管理员账号、角色、菜单等）。

```bash
cd ~/st/deploy

# 修改 .env
sed -i 's/APP_IS_DATA_SEED=false/APP_IS_DATA_SEED=true/' .env

# 重启应用服务（不重启基础设施）
docker compose -f docker-compose.2c2g.yml --env-file .env up -d \
  st-ms-identity-api \
  st-ms-operationlog-api \
  st-ms-operationlog-consumer \
  st-ms-fileupload-api \
  st-ms-order-api \
  st-ms-inventory-api \
  st-ms-payment-api \
  st-gateway

# 等几秒让种子数据写入，然后改回 false
sleep 10
sed -i 's/APP_IS_DATA_SEED=true/APP_IS_DATA_SEED=false/' .env
```

---

## 九、验证部署

### 9.1 检查所有容器状态

```bash
docker compose -f docker-compose.2c2g.yml ps
```

期望输出（11 个容器，全部 Up）：

```
NAME                         STATUS
postgres                     Up (healthy)
redis                        Up (healthy)
rabbitmq                     Up (healthy)
st-ms-identity-api           Up
st-ms-operationlog-api       Up
st-ms-operationlog-consumer  Up
st-ms-fileupload-api         Up
st-ms-order-api              Up
st-ms-inventory-api          Up
st-ms-payment-api            Up
st-gateway                   Up
st-web                       Up
```

### 9.2 测试 API

```bash
# Gateway 健康检查
curl http://127.0.0.1:25000/health

# 前端页面
curl -I http://127.0.0.1:280

# 测试登录接口（根据实际路由调整）
curl http://127.0.0.1:25000/api/identity/health
```

### 9.3 检查内存使用

```bash
docker stats --no-stream --format "table {{.Name}}\t{{.MemUsage}}\t{{.MemPerc}}"
```

---

## 十、配置防火墙

```bash
# 只开放必要端口
sudo ufw allow 22/tcp      # SSH
sudo ufw allow 80/tcp      # HTTP（前端）
sudo ufw allow 443/tcp     # HTTPS（后续加 Nginx 反代时）
sudo ufw allow 25000/tcp   # Gateway API（可选，如果前端直连）
sudo ufw enable
sudo ufw status

# ⚠️ 不要开放以下端口到公网:
# 25432 (PostgreSQL)
# 26379 (Redis)
# 25672 (RabbitMQ)
# 27127, 21001, 27250, 25090, 25091, 25092 (各微服务)
```

---

## 十一、后续优化（可选）

### 11.1 配置域名 + HTTPS

```bash
# 安装 Nginx + Certbot
sudo apt install -y nginx certbot python3-certbot-nginx

# 配置 Nginx 反向代理
sudo vim /etc/nginx/sites-available/st
```

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://127.0.0.1:280;
    }

    location /api/ {
        proxy_pass http://127.0.0.1:25000/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/st /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl enable --now nginx

# 申请证书
sudo certbot --nginx -d your-domain.com
```

### 11.2 设置自动部署（CI/CD）

镜像由 GitHub Actions 构建并推送至 ghcr.io，服务器只需拉取即可。

参考 `.github/workflows/build-images.yml`，配置 GitHub Actions 实现推代码自动构建并部署。

**部署流程：**
1. 推送代码到 `main` 分支
2. GitHub Actions 自动构建所有服务镜像并推送到 ghcr.io
3. 服务器上执行 `docker compose up -d --pull always` 拉取最新镜像并重启

### 11.3 日志查看

```bash
# 查看某个服务的日志
docker compose -f docker-compose.2c2g.yml logs -f st-ms-identity-api

# 查看所有服务最近 100 行
docker compose -f docker-compose.2c2g.yml logs --tail 100

# 搜索错误
docker compose -f docker-compose.2c2g.yml logs | grep -i error
```

### 11.4 数据备份

```bash
# PostgreSQL 备份（建议加到 crontab）
docker compose -f docker-compose.2c2g.yml exec -T postgres \
  pg_dumpall -U $PGUSER > backup_$(date +%Y%m%d).sql

# 每天凌晨 3 点自动备份
crontab -e
# 0 3 * * * cd ~/st/deploy && docker compose -f docker-compose.2c2g.yml exec -T postgres pg_dumpall -U st_user > /home/deploy/backups/db_$(date +\%Y\%m\%d).sql
```

---

## 快速命令速查

```bash
# 启动（拉取最新镜像并启动）
cd ~/st/deploy && docker compose -f docker-compose.2c2g.yml --env-file .env up -d --pull always

# 停止
cd ~/st/deploy && docker compose -f docker-compose.2c2g.yml down

# 重启某个服务
docker compose -f docker-compose.2c2g.yml restart st-ms-identity-api

# 查看状态
docker compose -f docker-compose.2c2g.yml ps

# 查看资源
docker stats --no-stream

# 查看日志
docker compose -f docker-compose.2c2g.yml logs -f --tail 50

# 拉取代码 → 更新镜像并重启（镜像由 GitHub Actions 构建）
cd ~/st && git pull
cd deploy && docker compose -f docker-compose.2c2g.yml --env-file .env up -d --pull always
```
