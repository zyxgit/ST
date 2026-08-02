#!/bin/bash
# ──────────────────────────────────────────────────────────────
# Let's Encrypt SSL 证书初始化脚本
#
# 使用方式:
#   chmod +x init-ssl.sh
#   ./init-ssl.sh yourdomain.com
#
# 前提条件:
#   1. 域名已解析到服务器 IP
#   2. 80 端口可访问（用于验证）
#   3. Docker 已安装并运行
# ──────────────────────────────────────────────────────────────

set -e

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 检查参数
if [ -z "$1" ]; then
    echo -e "${RED}错误: 请提供域名${NC}"
    echo "使用方式: ./init-ssl.sh yourdomain.com"
    echo "          ./init-ssl.sh yourdomain.com your@email.com"
    exit 1
fi

DOMAIN=$1
EMAIL=${2:-"admin@$DOMAIN"}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SSL_DIR="$SCRIPT_DIR/ssl"
CERTBOT_DIR="$SCRIPT_DIR/certbot"

echo -e "${GREEN}=== ST HTTPS 证书初始化 ===${NC}"
echo -e "域名: ${YELLOW}$DOMAIN${NC}"
echo -e "邮箱: ${YELLOW}$EMAIL${NC}"
echo ""

# 创建必要目录
mkdir -p "$SSL_DIR/live"
mkdir -p "$CERTBOT_DIR"

# 检查是否已有证书
if [ -f "$SSL_DIR/live/fullchain.pem" ]; then
    echo -e "${YELLOW}警告: 已存在证书文件${NC}"
    read -p "是否重新生成? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "已取消"
        exit 0
    fi
fi

# 步骤 1: 生成临时自签名证书（让 Nginx 能启动）
echo -e "${YELLOW}步骤 1: 生成临时自签名证书...${NC}"
openssl req -x509 -nodes -newkey rsa:2048 \
    -days 1 \
    -keyout "$SSL_DIR/live/privkey.pem" \
    -out "$SSL_DIR/live/fullchain.pem" \
    -subj "/CN=localhost" \
    2>/dev/null

echo -e "${GREEN}临时证书已生成${NC}"

# 步骤 2: 创建临时 Nginx 配置（用于验证）
echo -e "${YELLOW}步骤 2: 创建临时 Nginx 配置...${NC}"
cat > "$SCRIPT_DIR/nginx-temp.conf" << 'EOF'
worker_processes auto;
error_log /var/log/nginx/error.log warn;
pid /var/run/nginx.pid;

events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    server {
        listen 80;
        server_name _;

        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
        }

        location / {
            return 200 "OK";
            add_header Content-Type text/plain;
        }
    }
}
EOF

# 步骤 3: 启动临时 Nginx 进行验证
echo -e "${YELLOW}步骤 3: 启动临时 Nginx 进行域名验证...${NC}"
docker run -d \
    --name nginx-certbot-temp \
    -p 80:80 \
    -v "$SCRIPT_DIR/nginx-temp.conf:/etc/nginx/nginx.conf:ro" \
    -v "$CERTBOT_DIR:/var/www/certbot" \
    nginx:1.27-alpine

# 等待 Nginx 启动
sleep 2

# 步骤 4: 使用 Certbot 获取证书
echo -e "${YELLOW}步骤 4: 使用 Certbot 获取 Let's Encrypt 证书...${NC}"
docker run --rm \
    -v "$SSL_DIR:/etc/letsencrypt" \
    -v "$CERTBOT_DIR:/var/www/certbot" \
    certbot/certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email "$EMAIL" \
    --agree-tos \
    --no-eff-email \
    -d "$DOMAIN"

# 步骤 5: 停止临时 Nginx
echo -e "${YELLOW}步骤 5: 停止临时 Nginx...${NC}"
docker stop nginx-certbot-temp
docker rm nginx-certbot-temp

# 步骤 6: 复制证书到目标位置
echo -e "${YELLOW}步骤 6: 复制证书到目标位置...${NC}"
if [ -d "$SSL_DIR/live/$DOMAIN" ]; then
    cp "$SSL_DIR/live/$DOMAIN/fullchain.pem" "$SSL_DIR/live/fullchain.pem"
    cp "$SSL_DIR/live/$DOMAIN/privkey.pem" "$SSL_DIR/live/privkey.pem"
    echo -e "${GREEN}证书已复制${NC}"
else
    echo -e "${RED}错误: 证书生成失败，请检查域名解析和 80 端口是否可访问${NC}"
    exit 1
fi

# 清理临时文件
rm -f "$SCRIPT_DIR/nginx-temp.conf"

echo ""
echo -e "${GREEN}=== SSL 证书初始化完成 ===${NC}"
echo ""
echo "证书位置:"
echo "  - 证书链: $SSL_DIR/live/fullchain.pem"
echo "  - 私钥:   $SSL_DIR/live/privkey.pem"
echo ""
echo "下一步:"
echo "  1. 更新 nginx.conf 中的 server_name 为: $DOMAIN"
echo "  2. 启动服务: docker compose -f docker-compose.2c2g.https.yml --env-file .env up -d"
echo "  3. 访问: https://$DOMAIN"
echo ""
echo -e "${YELLOW}证书自动续期已配置在 certbot 容器中，无需手动操作${NC}"
