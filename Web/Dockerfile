# ---- Build stage ----
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json pnpm-lock.yaml ./
RUN --mount=type=cache,id=pnpm,target=/root/.pnpm-store \
    corepack enable && \
    pnpm config set store-dir /root/.pnpm-store && \
    pnpm install --frozen-lockfile
COPY . .
RUN corepack enable && pnpm run build

# ---- Runtime stage ----
FROM nginx:stable-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
