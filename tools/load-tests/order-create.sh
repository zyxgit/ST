#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────
# 并发下单压测脚本
# 用法：bash order-create.sh [并发数] [总请求数] [Gateway地址]
# 示例：bash order-create.sh 50 200 http://localhost:25000
# ─────────────────────────────────────────────────────────────
set -euo pipefail

CONCURRENCY=${1:-50}
TOTAL_REQUESTS=${2:-200}
GATEWAY_URL=${3:-"http://localhost:25000"}
SKU_ID="00000000-0000-0000-0000-000000000001"
INITIAL_STOCK=$((TOTAL_REQUESTS + 100))  # 库存略大于请求数

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"
echo -e "${CYAN}  ST 并发下单压测${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"
echo -e "  Gateway:      ${GATEWAY_URL}"
echo -e "  并发数:       ${CONCURRENCY}"
echo -e "  总请求数:     ${TOTAL_REQUESTS}"
echo -e "  初始库存:     ${INITIAL_STOCK}"
echo -e "  SKU ID:       ${SKU_ID}"
echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"
echo ""

# ── 1. 创建 SKU（幂等，已存在则跳过） ──
echo -e "${YELLOW}[1/4] 创建 SKU...${NC}"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -X POST "${GATEWAY_URL}/api/inventory/skus" \
  -H "Content-Type: application/json" \
  -d "{\"skuId\":\"${SKU_ID}\",\"productName\":\"压测商品\",\"initialStock\":${INITIAL_STOCK}}" \
  2>/dev/null || echo "000")

if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
  echo -e "  ${GREEN}✓ SKU 创建成功 (HTTP ${HTTP_CODE})${NC}"
elif [ "$HTTP_CODE" = "409" ] || [ "$HTTP_CODE" = "400" ]; then
  echo -e "  ${YELLOW}⚠ SKU 已存在，跳过创建${NC}"
else
  echo -e "  ${RED}✗ SKU 创建失败 (HTTP ${HTTP_CODE})${NC}"
  echo -e "  ${YELLOW}提示：请确保 Inventory 服务已启动且 Gateway 已配置路由${NC}"
  exit 1
fi

# ── 2. 增加库存（确保足够） ──
echo -e "${YELLOW}[2/4] 增加库存至 ${INITIAL_STOCK}...${NC}"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -X POST "${GATEWAY_URL}/api/inventory/skus/${SKU_ID}/stock/increase" \
  -H "Content-Type: application/json" \
  -d "{\"quantity\":${INITIAL_STOCK}}" \
  2>/dev/null || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
  echo -e "  ${GREEN}✓ 库存增加成功 (HTTP ${HTTP_CODE})${NC}"
else
  echo -e "  ${YELLOW}⚠ 库存增加返回 HTTP ${HTTP_CODE}（可能库存已充足）${NC}"
fi
echo ""

# ── 3. 并发下单 ──
echo -e "${YELLOW}[3/4] 开始并发下单（${CONCURRENCY} 并发，共 ${TOTAL_REQUESTS} 请求）...${NC}"

TMPDIR=$(mktemp -d)
RESULT_FILE="${TMPDIR}/results.txt"
ERROR_FILE="${TMPDIR}/errors.txt"
TIMING_FILE="${TMPDIR}/timings.txt"

> "${RESULT_FILE}"
> "${ERROR_FILE}"
> "${TIMING_FILE}"

START_TIME=$(date +%s%N)

# 定义下单函数
place_order() {
  local i=$1
  local user_id="00000000-0000-0000-0000-$(printf '%012d' $i)"
  local start_ms=$(date +%s%N)

  local http_code
  http_code=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "${GATEWAY_URL}/api/orders" \
    -H "Content-Type: application/json" \
    -d "{\"userId\":\"${user_id}\",\"items\":[{\"skuId\":\"${SKU_ID}\",\"productName\":\"压测商品\",\"quantity\":1,\"unitPrice\":99.99}]}" \
    --connect-timeout 10 \
    --max-time 30 \
    2>/dev/null || echo "000")

  local end_ms=$(date +%s%N)
  local duration_ms=$(( (end_ms - start_ms) / 1000000 ))

  echo "${http_code}" >> "${RESULT_FILE}"
  echo "${duration_ms}" >> "${TIMING_FILE}"

  if [ "$http_code" != "200" ] && [ "$http_code" != "201" ]; then
    echo "Request ${i}: HTTP ${http_code}" >> "${ERROR_FILE}"
  fi
}

# 导出函数和变量供子 shell 使用
export -f place_order
export GATEWAY_URL SKU_ID RESULT_FILE ERROR_FILE TIMING_FILE

# 使用并发执行
RUNNING=0
for i in $(seq 1 "${TOTAL_REQUESTS}"); do
  place_order "${i}" &
  RUNNING=$((RUNNING + 1))

  if [ "${RUNNING}" -ge "${CONCURRENCY}" ]; then
    wait -n 2>/dev/null || wait
    RUNNING=$((RUNNING - 1))
  fi
done

# 等待所有请求完成
wait

END_TIME=$(date +%s%N)
TOTAL_DURATION_MS=$(( (END_TIME - START_TIME) / 1000000 ))

# ── 4. 统计结果 ──
echo ""
echo -e "${YELLOW}[4/4] 统计结果${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"

TOTAL_COUNT=$(wc -l < "${RESULT_FILE}" | tr -d ' ')
SUCCESS_COUNT=$(grep -c "^20[01]$" "${RESULT_FILE}" 2>/dev/null || echo "0")
FAIL_COUNT=$((TOTAL_COUNT - SUCCESS_COUNT))
ERROR_COUNT=$(wc -l < "${ERROR_FILE}" | tr -d ' ')

# 响应时间统计
if [ -s "${TIMING_FILE}" ]; then
  SORTED_TIMINGS=$(sort -n "${TIMING_FILE}")
  AVG_MS=$(awk '{sum+=$1} END {printf "%.0f", sum/NR}' "${TIMING_FILE}")
  MIN_MS=$(head -1 <<< "${SORTED_TIMINGS}")
  MAX_MS=$(tail -1 <<< "${SORTED_TIMINGS}")
  P50_LINE=$(( TOTAL_COUNT * 50 / 100 + 1 ))
  P95_LINE=$(( TOTAL_COUNT * 95 / 100 + 1 ))
  P99_LINE=$(( TOTAL_COUNT * 99 / 100 + 1 ))
  P50_MS=$(sed -n "${P50_LINE}p" <<< "${SORTED_TIMINGS}")
  P95_MS=$(sed -n "${P95_LINE}p" <<< "${SORTED_TIMINGS}")
  P99_MS=$(sed -n "${P99_LINE}p" <<< "${SORTED_TIMINGS}")
else
  AVG_MS=0; MIN_MS=0; MAX_MS=0; P50_MS=0; P95_MS=0; P99_MS=0
fi

# TPS
if [ "${TOTAL_DURATION_MS}" -gt 0 ]; then
  TPS=$(awk "BEGIN {printf \"%.1f\", ${SUCCESS_COUNT} * 1000 / ${TOTAL_DURATION_MS}}")
else
  TPS="N/A"
fi

# 成功率
if [ "${TOTAL_COUNT}" -gt 0 ]; then
  SUCCESS_RATE=$(awk "BEGIN {printf \"%.1f\", ${SUCCESS_COUNT} * 100 / ${TOTAL_COUNT}}")
else
  SUCCESS_RATE="0"
fi

echo -e "  总请求数:     ${CYAN}${TOTAL_COUNT}${NC}"
echo -e "  成功数:       ${GREEN}${SUCCESS_COUNT}${NC}"
echo -e "  失败数:       ${RED}${FAIL_COUNT}${NC}"
echo -e "  成功率:       ${GREEN}${SUCCESS_RATE}%${NC}"
echo -e "  总耗时:       ${CYAN}${TOTAL_DURATION_MS}ms${NC}"
echo -e "  TPS:          ${CYAN}${TPS}${NC}"
echo ""
echo -e "  响应时间:"
echo -e "    最小:       ${MIN_MS}ms"
echo -e "    最大:       ${MAX_MS}ms"
echo -e "    平均:       ${AVG_MS}ms"
echo -e "    P50:        ${P50_MS}ms"
echo -e "    P95:        ${P95_MS}ms"
echo -e "    P99:        ${P99_MS}ms"
echo -e "${CYAN}═══════════════════════════════════════════════════${NC}"

# 打印部分错误信息
if [ "${ERROR_COUNT}" -gt 0 ]; then
  echo ""
  echo -e "${RED}错误详情（前 10 条）：${NC}"
  head -10 "${ERROR_FILE}" | while read -r line; do
    echo -e "  ${RED}${line}${NC}"
  done
fi

# 清理
rm -rf "${TMPDIR}"

echo ""
echo -e "${GREEN}压测完成！${NC}"
