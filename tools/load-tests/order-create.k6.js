// ─────────────────────────────────────────────────────────────
// ST 并发下单压测 (k6)
// 用法：k6 run --env GATEWAY_URL=http://localhost:25000 order-create.k6.js
// ─────────────────────────────────────────────────────────────
import http from "k6/http";
import { check, sleep } from "k6";
import { Counter, Trend } from "k6/metrics";

// 自定义指标
const orderSuccess = new Counter("order_success");
const orderFail = new Counter("order_fail");
const orderDuration = new Trend("order_duration", true);

// 配置
const GATEWAY_URL = __ENV.GATEWAY_URL || "http://localhost:25000";
const SKU_ID = "00000000-0000-0000-0000-000000000001";

export const options = {
  scenarios: {
    // 阶梯负载：10 → 50 → 100 VU
    ramp_up: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "10s", target: 10 },
        { duration: "30s", target: 10 },
        { duration: "10s", target: 50 },
        { duration: "30s", target: 50 },
        { duration: "10s", target: 100 },
        { duration: "30s", target: 100 },
        { duration: "10s", target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_duration: ["p(95)<5000"], // 95% 请求应在 5 秒内完成
    order_success: ["count>0"], // 至少有成功的订单
  },
};

// 初始化：创建 SKU + 增加库存
export function setup() {
  const headers = { "Content-Type": "application/json" };

  // 创建 SKU
  const skuRes = http.post(
    `${GATEWAY_URL}/api/inventory/skus`,
    JSON.stringify({
      skuId: SKU_ID,
      productName: "k6压测商品",
      initialStock: 10000,
    }),
    { headers }
  );

  // 增加库存
  http.post(
    `${GATEWAY_URL}/api/inventory/skus/${SKU_ID}/stock/increase`,
    JSON.stringify({ quantity: 10000 }),
    { headers }
  );

  return { skuCreated: skuRes.status === 200 || skuRes.status === 201 };
}

export default function (data) {
  const userId = `00000000-0000-0000-0000-${String(__VU).padStart(4, "0")}${String(__ITER).padStart(8, "0")}`;

  const payload = JSON.stringify({
    userId: userId,
    items: [
      {
        skuId: SKU_ID,
        productName: "k6压测商品",
        quantity: 1,
        unitPrice: 99.99,
      },
    ],
  });

  const headers = { "Content-Type": "application/json" };

  const res = http.post(`${GATEWAY_URL}/api/orders`, payload, { headers });

  const duration = res.timings.duration;
  orderDuration.add(duration);

  const success = check(res, {
    "order created (200/201)": (r) => r.status === 200 || r.status === 201,
  });

  if (success) {
    orderSuccess.add(1);
  } else {
    orderFail.add(1);
  }

  sleep(0.1);
}

// 输出汇总结果
export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    total_requests: data.metrics.http_reqs?.values?.count || 0,
    order_success: data.metrics.order_success?.values?.count || 0,
    order_fail: data.metrics.order_fail?.values?.count || 0,
    avg_duration_ms: data.metrics.http_req_duration?.values?.avg?.toFixed(1) || 0,
    p50_duration_ms: data.metrics.http_req_duration?.values?.["p(50)"]?.toFixed(1) || 0,
    p95_duration_ms: data.metrics.http_req_duration?.values?.["p(95)"]?.toFixed(1) || 0,
    p99_duration_ms: data.metrics.http_req_duration?.values?.["p(99)"]?.toFixed(1) || 0,
    iterations: data.metrics.iterations?.values?.count || 0,
  };

  return {
    stdout: `
═══════════════════════════════════════════════════
  ST 下单压测结果
═══════════════════════════════════════════════════
  总请求数:     ${summary.total_requests}
  下单成功:     ${summary.order_success}
  下单失败:     ${summary.order_fail}
  迭代次数:     ${summary.iterations}

  响应时间:
    平均:       ${summary.avg_duration_ms}ms
    P50:        ${summary.p50_duration_ms}ms
    P95:        ${summary.p95_duration_ms}ms
    P99:        ${summary.p99_duration_ms}ms
═══════════════════════════════════════════════════
`,
    "tools/load-tests/order-create-summary.json": JSON.stringify(summary, null, 2),
  };
}
