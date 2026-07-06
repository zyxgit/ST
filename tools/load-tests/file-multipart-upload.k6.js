// ─────────────────────────────────────────────────────────────
// ST 文件上传压测 (k6)
// 测试普通文件上传接口
// 用法：k6 run --env GATEWAY_URL=http://localhost:25000 file-multipart-upload.k6.js
// ─────────────────────────────────────────────────────────────
import http from "k6/http";
import { check, sleep } from "k6";
import { Counter, Trend } from "k6/metrics";

// 自定义指标
const uploadSuccess = new Counter("upload_success");
const uploadFail = new Counter("upload_fail");
const uploadDuration = new Trend("upload_duration", true);

const GATEWAY_URL = __ENV.GATEWAY_URL || "http://localhost:25000";

export const options = {
  scenarios: {
    // 10 个 VU，每个上传 5 个文件
    upload: {
      executor: "per-vu-iterations",
      vus: 10,
      iterations: 5,
      maxDuration: "2m",
    },
  },
  thresholds: {
    upload_success: ["count>0"],
    upload_duration: ["p(95)<10000"], // 95% 上传应在 10 秒内完成
  },
};

export default function () {
  // 生成一个小文件内容（~1KB）
  const fileContent = "A".repeat(1024);

  const blob = http.file(fileContent, `test-file-${__VU}-${__ITER}.txt`, "text/plain");

  const res = http.post(
    `${GATEWAY_URL}/api/files/upload`,
    { file: blob },
    {
      headers: {
        // k6 自动设置 Content-Type: multipart/form-data
      },
    }
  );

  uploadDuration.add(res.timings.duration);

  const success = check(res, {
    "upload succeeded (200/201)": (r) => r.status === 200 || r.status === 201,
  });

  if (success) {
    uploadSuccess.add(1);
  } else {
    uploadFail.add(1);
  }

  sleep(0.5);
}

export function handleSummary(data) {
  const summary = {
    timestamp: new Date().toISOString(),
    total_requests: data.metrics.http_reqs?.values?.count || 0,
    upload_success: data.metrics.upload_success?.values?.count || 0,
    upload_fail: data.metrics.upload_fail?.values?.count || 0,
    avg_duration_ms: data.metrics.http_req_duration?.values?.avg?.toFixed(1) || 0,
    p50_duration_ms: data.metrics.http_req_duration?.values?.["p(50)"]?.toFixed(1) || 0,
    p95_duration_ms: data.metrics.http_req_duration?.values?.["p(95)"]?.toFixed(1) || 0,
    p99_duration_ms: data.metrics.http_req_duration?.values?.["p(99)"]?.toFixed(1) || 0,
  };

  return {
    stdout: `
═══════════════════════════════════════════════════
  ST 文件上传压测结果
═══════════════════════════════════════════════════
  总请求数:     ${summary.total_requests}
  上传成功:     ${summary.upload_success}
  上传失败:     ${summary.upload_fail}

  响应时间:
    平均:       ${summary.avg_duration_ms}ms
    P50:        ${summary.p50_duration_ms}ms
    P95:        ${summary.p95_duration_ms}ms
    P99:        ${summary.p99_duration_ms}ms
═══════════════════════════════════════════════════
`,
    "tools/load-tests/file-upload-summary.json": JSON.stringify(summary, null, 2),
  };
}
