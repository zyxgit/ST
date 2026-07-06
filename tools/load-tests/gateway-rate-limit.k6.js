// ─────────────────────────────────────────────────────────────
// ST Gateway 限流压测 (k6)
// 高频请求同一接口触发限流，验证 429 + Retry-After
// 用法：k6 run --env GATEWAY_URL=http://localhost:25000 gateway-rate-limit.k6.js
// ─────────────────────────────────────────────────────────────
import http from "k6/http";
import { check, sleep } from "k6";
import { Counter, Trend } from "k6/metrics";

// 自定义指标
const success = new Counter("rate_success");
const rateLimited = new Counter("rate_limited");
const reqDuration = new Trend("req_duration", true);

const GATEWAY_URL = __ENV.GATEWAY_URL || "http://localhost:25000";

export const options = {
  scenarios: {
    // 短时间高并发冲击限流
    burst: {
      executor: "constant-vus",
      vus: 50,
      duration: "30s",
    },
  },
  thresholds: {
    rate_limited: ["count>0"], // 应该触发限流
  },
};

export default function () {
  // 请求一个轻量级接口（健康检查或 docs）
  const res = http.get(`${GATEWAY_URL}/health`, {
    tags: { name: "health-check" },
  });

  reqDuration.add(res.timings.duration);

  if (res.status === 429) {
    rateLimited.add(1);
    // 验证 Retry-After header
    check(res, {
      "has Retry-After header": (r) => r.headers["Retry-After"] !== undefined,
    });
  } else if (res.status === 200) {
    success.add(1);
  }

  sleep(0.05); // 极短间隔，最大化请求密度
}

export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    total_requests: data.metrics.http_reqs?.values?.count || 0,
    rate_success: data.metrics.rate_success?.values?.count || 0,
    rate_limited: data.metrics.rate_limited?.values?.count || 0,
    avg_duration_ms: data.metrics.http_req_duration?.values?.avg?.toFixed(1) || 0,
    p95_duration_ms: data.metrics.http_req_duration?.values?.["p(95)"]?.toFixed(1) || 0,
  };

  return {
    stdout: `
═══════════════════════════════════════════════════
  ST Gateway 限流压测结果
═══════════════════════════════════════════════════
  总请求数:     ${summary.total_requests}
  成功 (200):   ${summary.rate_success}
  限流 (429):   ${summary.rate_limited}
  限流比例:     ${summary.total_requests > 0 ? ((summary.rate_limited / summary.total_requests) * 100).toFixed(1) : 0}%

  响应时间:
    平均:       ${summary.avg_duration_ms}ms
    P95:        ${summary.p95_duration_ms}ms
═══════════════════════════════════════════════════
`,
    "tools/load-tests/gateway-rate-limit-summary.json": JSON.stringify(summary, null, 2),
  };
}
