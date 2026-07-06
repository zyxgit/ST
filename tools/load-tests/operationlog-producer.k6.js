// ─────────────────────────────────────────────────────────────
// ST 操作日志生成压测 (k6)
// 通过高频 API 请求触发 OperationLogActionFilter 生成审计日志
// 用法：k6 run --env GATEWAY_URL=http://localhost:25000 operationlog-producer.k6.js
// ─────────────────────────────────────────────────────────────
import http from "k6/http";
import { check, sleep } from "k6";
import { Counter, Trend } from "k6/metrics";

// 自定义指标
const logGenerated = new Counter("log_generated");
const logFailed = new Counter("log_failed");
const reqDuration = new Trend("req_duration", true);

const GATEWAY_URL = __ENV.GATEWAY_URL || "http://localhost:25000";

export const options = {
  scenarios: {
    // 20 VU，持续 30 秒，高频请求产生操作日志
    producer: {
      executor: "constant-vus",
      vus: 20,
      duration: "30s",
    },
  },
  thresholds: {
    log_generated: ["count>0"],
    http_req_duration: ["p(95)<3000"],
  },
};

export default function () {
  // 请求 Identity 服务的用户列表接口（会产生操作日志）
  const res = http.get(`${GATEWAY_URL}/api/identity/user/list`, {
    headers: { "Content-Type": "application/json" },
    tags: { name: "identity-user-list" },
  });

  reqDuration.add(res.timings.duration);

  const success = check(res, {
    "request succeeded (200)": (r) => r.status === 200,
    "is not rate limited": (r) => r.status !== 429,
  });

  if (success) {
    logGenerated.add(1);
  } else {
    logFailed.add(1);
  }

  sleep(0.1);
}

export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    total_requests: data.metrics.http_reqs?.values?.count || 0,
    log_generated: data.metrics.log_generated?.values?.count || 0,
    log_failed: data.metrics.log_failed?.values?.count || 0,
    avg_duration_ms: data.metrics.http_req_duration?.values?.avg?.toFixed(1) || 0,
    p50_duration_ms: data.metrics.http_req_duration?.values?.["p(50)"]?.toFixed(1) || 0,
    p95_duration_ms: data.metrics.http_req_duration?.values?.["p(95)"]?.toFixed(1) || 0,
    p99_duration_ms: data.metrics.http_req_duration?.values?.["p(99)"]?.toFixed(1) || 0,
    iterations: data.metrics.iterations?.values?.count || 0,
  };

  return {
    stdout: `
═══════════════════════════════════════════════════
  ST 操作日志生成压测结果
═══════════════════════════════════════════════════
  总请求数:     ${summary.total_requests}
  日志生成成功: ${summary.log_generated}
  日志生成失败: ${summary.log_failed}
  迭代次数:     ${summary.iterations}

  响应时间:
    平均:       ${summary.avg_duration_ms}ms
    P50:        ${summary.p50_duration_ms}ms
    P95:        ${summary.p95_duration_ms}ms
    P99:        ${summary.p99_duration_ms}ms
═══════════════════════════════════════════════════
`,
    "tools/load-tests/operationlog-producer-summary.json": JSON.stringify(summary, null, 2),
  };
}
