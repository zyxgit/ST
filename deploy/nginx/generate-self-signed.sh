#!/bin/bash
# ──────────────────────────────────────────────────────────────
# 自签名 SSL 证书生成脚本（测试用）
#
# 使用方式:
#   chmod +x generate-self-signed.sh
#   ./generate-self-signed.sh
#   ./generate-self-signed.sh yourdomain.com
#
# 注意: 自签名证书浏览器会显示安全警告，仅用于测试
# ──────────────────────────────────────────────────────────────

set -e

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

DOMAIN=${1:-"localhost"}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SSL_DIR="$SCRIPT_DIR/ssl"

echo -e "${GREEN}=== 生成自签名 SSL 证书 ===${NC}"
echo -e "域名: ${YELLOW}$DOMAIN${NC}"
echo ""

# 创建目录
mkdir -p "$SSL_DIR/live"

# 检查是否已有证书
if [ -f "$SSL_DIR/live/self-signed.crt" ]; then
    echo -e "${YELLOW}警告: 已存在自签名证书${NC}"
    read -p "是否重新生成? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "已取消"
        exit 0
    fi
fi

# 生成私钥和证书
echo -e "${YELLOW}生成证书...${NC}"
openssl req -x509 -nodes -newkey rsa:2048 \
    -days 365 \
    -keyout "$SSL_DIR/live/self-signed.key" \
    -out "$SSL_DIR/live/self-signed.crt" \
    -subj "/C=CN/ST=State/L=City/O=Organization/CN=$DOMAIN" \
    -addext "subjectAltName=DNS:$DOMAIN,DNS:localhost,IP:127.0.0.1"

echo -e "${GREEN}自签名证书已生成${NC}"

# 创建符号链接，方便 Nginx 配置使用
ln -sf "$SSL_DIR/live/self-signed.crt" "$SSL_DIR/live/fullchain.pem"
ln -sf "$SSL_DIR/live/self-signed.key" "$SSL_DIR/live/privkey.pem"

echo ""
echo -e "${GREEN}=== 完成 ===${NC}"
echo ""
echo "证书文件:"
echo "  - 证书: $SSL_DIR/live/self-signed.crt"
echo "  - 私钥: $SSL_DIR/live/self-signed.key"
echo ""
echo "已创建符号链接:"
echo "  - fullchain.pem -> self-signed.crt"
echo "  - privkey.pem -> self-signed.key"
echo ""
echo -e "${YELLOW}注意: 自签名证书会导致浏览器安全警告${NC}"
echo -e "${YELLOW}仅建议用于开发测试环境${NC}"
echo ""
echo "下一步:"
echo "  1. 确保 nginx.conf 使用的是自签名证书配置（取消注释方式2）"
echo "  2. 启动服务: docker compose -f docker-compose.2c2g.https.yml --env-file .env up -d"
echo "  3. 访问: https://$DOMAIN（接受安全警告即可）"
