# ST HTTPS 配置指南

## 文件结构

```
deploy/
├── nginx/
│   ├── nginx.conf              # Nginx 主配置
│   ├── init-ssl.sh             # Let's Encrypt 证书初始化脚本
│   ├── generate-self-signed.sh # 自签名证书生成脚本（测试用）
│   ├── ssl/                    # SSL 证书目录（自动生成）
│   ├── certbot/                # Certbot 验证目录（自动生成）
│   └── README.md               # 本文件
├── docker-compose.2c2g.https.yml  # HTTPS 版本的 Docker Compose
└── .env.2c2g.example           # 环境变量示例（已添加 DOMAIN_NAME）
```

## 快速开始

### 方式一：Let's Encrypt 证书（推荐，需要域名）

**前提条件：**
- 域名已解析到服务器 IP
- 服务器 80 端口可从外网访问
- 已安装 Docker

**步骤：**

1. **配置环境变量**
   ```bash
   cd deploy
   cp .env.2c2g.example .env
   # 编辑 .env，设置 DOMAIN_NAME=yourdomain.com
   ```

2. **生成 SSL 证书**
   ```bash
   cd nginx
   chmod +x init-ssl.sh
   ./init-ssl.sh yourdomain.com
   ```

3. **更新 Nginx 配置**
   ```bash
   # 编辑 nginx/nginx.conf，将 server_name _ 改为:
   # server_name yourdomain.com;
   ```

4. **启动服务**
   ```bash
   cd deploy
   docker compose -f docker-compose.2c2g.https.yml --env-file .env up -d
   ```

5. **访问**
   ```
   https://yourdomain.com
   ```

### 方式二：自签名证书（测试用）

**适用场景：**
- 本地开发测试
- 内网环境
- 不需要浏览器信任的证书

**步骤：**

1. **生成自签名证书**
   ```bash
   cd nginx
   chmod +x generate-self-signed.sh
   ./generate-self-signed.sh localhost
   ```

2. **修改 Nginx 配置**
   
   编辑 `nginx/nginx.conf`，注释掉 Let's Encrypt 证书配置，启用自签名证书：
   
   ```nginx
   # SSL 证书配置
   # 方式1: Let's Encrypt 证书（推荐）
   # ssl_certificate /etc/nginx/ssl/live/fullchain.pem;
   # ssl_certificate_key /etc/nginx/ssl/live/privkey.pem;

   # 方式2: 自签名证书（测试用，取消注释替换上面的配置）
   ssl_certificate /etc/nginx/ssl/self-signed.crt;
   ssl_certificate_key /etc/nginx/ssl/self-signed.key;
   ```

3. **启动服务**
   ```bash
   cd deploy
   docker compose -f docker-compose.2c2g.https.yml --env-file .env up -d
   ```

4. **访问**
   ```
   https://localhost  # 浏览器会显示安全警告，点击"高级"->"继续访问"
   ```

## 端口说明

使用 HTTPS 后，只需要开放以下端口：

| 端口 | 用途 | 防火墙设置 |
|------|------|-----------|
| 80 | HTTP → HTTPS 重定向 | 必须开放 |
| 443 | HTTPS 访问 | 必须开放 |
| 25432 | PostgreSQL | 可选（调试用） |
| 26379 | Redis | 可选（调试用） |
| 25672 | RabbitMQ | 可选（调试用） |

**注意：** 前端 (28080) 和网关 (25000) 端口不再需要对外暴露。

## 访问路径

配置完成后，通过以下路径访问：

| 路径 | 服务 | 说明 |
|------|------|------|
| `/` | st-web | 前端页面 |
| `/api/*` | st-gateway | 后端 API |
| `/api/fileupload/*` | st-gateway | 文件上传 |

## 证书续期

### Let's Encrypt 证书

证书有效期为 90 天，`certbot` 容器会自动每 12 小时检查并续期，无需手动操作。

如需手动续期：
```bash
docker compose -f docker-compose.2c2g.https.yml --env-file .env run --rm certbot renew
```

### 自签名证书

自签名证书有效期为 365 天，到期后需要重新生成：
```bash
cd nginx
./generate-self-signed.sh
docker compose -f docker-compose.2c2g.https.yml --env-file .env restart nginx
```

## 故障排查

### 1. 证书生成失败

**问题：** `init-ssl.sh` 报错

**可能原因：**
- 域名未解析到服务器 IP
- 80 端口被占用或防火墙未开放
- 域名 DNS 未生效

**解决方案：**
```bash
# 检查域名解析
dig yourdomain.com

# 检查 80 端口
netstat -tlnp | grep :80

# 检查防火墙
sudo ufw status  # Ubuntu
sudo firewall-cmd --list-all  # CentOS
```

### 2. Nginx 启动失败

**问题：** `docker compose up` 后 Nginx 退出

**查看日志：**
```bash
docker compose -f docker-compose.2c2g.https.yml logs nginx
```

**常见原因：**
- 证书文件路径错误
- nginx.conf 语法错误
- 端口 80/443 被占用

### 3. 浏览器显示"不安全"

**Let's Encrypt 证书：**
- 检查证书是否过期
- 确认域名与证书匹配
- 清除浏览器缓存

**自签名证书：**
- 这是正常现象，点击"高级"->"继续访问"即可
- 或者将证书导入系统信任库

### 4. API 请求 404

**问题：** 前端能访问，但 API 请求失败

**检查：**
- 确认 `st-gateway` 容器正常运行
- 检查 Nginx 配置中的 `proxy_pass` 路径
- 查看网关日志：`docker compose logs st-gateway`

## 高级配置

### 自定义域名

编辑 `nginx/nginx.conf`，修改 `server_name`：

```nginx
server_name yourdomain.com www.yourdomain.com;
```

### 多域名支持

如果需要支持多个域名，在 `server_name` 中添加：

```nginx
server_name domain1.com domain2.com *.domain1.com;
```

### 自定义上传大小限制

编辑 `nginx/nginx.conf`，修改 `client_max_body_size`：

```nginx
# 全局设置
client_max_body_size 100m;

# 文件上传路径单独设置
location /api/fileupload/ {
    client_max_body_size 200m;
    # ...
}
```

### 性能调优

```nginx
# 增加 worker 连接数
events {
    worker_connections 2048;
}

# 启用 HTTP/2
listen 443 ssl http2;

# 调整缓冲区
proxy_buffer_size 8k;
proxy_buffers 16 8k;
```

## 安全建议

1. **定期更新证书**：Let's Encrypt 自动续期，自签名证书手动更新
2. **限制管理端口**：PostgreSQL、Redis、RabbitMQ 端口仅内网访问
3. **启用防火墙**：只开放 80 和 443 端口
4. **使用强密码**：数据库、Redis、RabbitMQ 使用强密码
5. **定期备份**：备份数据库和上传文件
6. **监控日志**：定期检查 Nginx 和应用日志
