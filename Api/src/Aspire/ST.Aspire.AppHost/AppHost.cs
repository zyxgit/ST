using System.Text;

Console.OutputEncoding = Encoding.UTF8;
var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env");

IResourceBuilder<RedisResource> redis;
IResourceBuilder<PostgresServerResource> postgres;
IResourceBuilder<RabbitMQServerResource> rabbitMq;

var password = builder.AddParameter("password");

var pguser = builder.AddParameter("pguser");

var rabbitUser = builder.AddParameter("rabbitUser");
var rabbitPassword = builder.AddParameter("rabbitPassword");

redis = builder.AddRedis("cache", 16379)
	.WithHostPort(16379)
	.WithPassword(password)
	.WithDataVolume()
	.WithLifetime(ContainerLifetime.Persistent);


postgres = builder.AddPostgres("postgres", pguser, port: 15432)
	.WithHostPort(15432)
	.WithPassword(password)
	.WithDataVolume()
	.WithLifetime(ContainerLifetime.Persistent);

var testDb = postgres.AddDatabase("st-test", "st_test");
var fileUploadDb = postgres.AddDatabase("st-fileupload", "st_fileupload");
var identityDb = postgres.AddDatabase("st-identity", "st_identity");
var operationLogDb = postgres.AddDatabase("st-operationlog", "st_operationlog");
var orderDb = postgres.AddDatabase("st-order", "st_order");
var inventoryDb = postgres.AddDatabase("st-inventory", "st_inventory");
var paymentDb = postgres.AddDatabase("st-payment", "st_payment");

rabbitMq = builder.AddRabbitMQ("rabbitmq", rabbitUser, rabbitPassword, port: 5672)
	.WithDataVolume()
	.WithManagementPlugin(port: 15672)
	.WithLifetime(ContainerLifetime.Persistent);

// ── 可观测性栈 ──────────────────────────────────────────────────────────────

// Prometheus - 指标存储
var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.3.0")
	.WithArgs(
		"--config.file=/etc/prometheus/prometheus.yml",
		"--storage.tsdb.path=/prometheus",
		"--web.enable-remote-write-receiver",
		"--enable-feature=otlp-write-receiver"
	)
	.WithBindMount("../../../../deploy/prometheus/prometheus.yml", "/etc/prometheus/prometheus.yml", true)
	.WithVolume("prometheus-data", "/prometheus")
	.WithEndpoint(29090, 9090, name: "http")
	.WithLifetime(ContainerLifetime.Persistent);

// Loki - 日志存储
var loki = builder.AddContainer("loki", "grafana/loki", "3.4.2")
	.WithBindMount("../../../../deploy/loki/loki-config.yaml", "/etc/loki/loki-config.yaml", true)
	.WithVolume("loki-data", "/loki")
	.WithEndpoint(23100, 3100, name: "http")
	.WithLifetime(ContainerLifetime.Persistent);

// Alloy - OpenTelemetry Collector
var alloy = builder.AddContainer("alloy", "grafana/alloy", "v1.8.1")
	.WithArgs("run", "--storage.path=/var/lib/alloy/data", "/etc/alloy/config.alloy")
	.WithBindMount("../../../../deploy/alloy/config.alloy", "/etc/alloy/config.alloy", true)
	.WithVolume("alloy-data", "/var/lib/alloy/data")
	.WithEndpoint(24317, 4317, name: "otlp-grpc")
	.WithEndpoint(24318, 4318, name: "otlp-http")
	.WithEndpoint(12345, 12345, name: "http")
	.WithLifetime(ContainerLifetime.Persistent);

// Grafana - 可视化面板
var grafana = builder.AddContainer("grafana", "grafana/grafana", "11.5.2")
	.WithEnvironment("GF_SECURITY_ADMIN_USER", "admin")
	.WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin123")
	.WithBindMount("../../../../deploy/grafana/datasources", "/etc/grafana/provisioning/datasources", true)
	.WithBindMount("../../../../deploy/grafana/provisioning/dashboards", "/etc/grafana/provisioning/dashboards", true)
	.WithVolume("grafana-data", "/var/lib/grafana")
	.WithEndpoint(23000, 3000, name: "http")
	.WaitFor(prometheus)
	.WaitFor(loki)
	.WithLifetime(ContainerLifetime.Persistent);

// ── 业务服务 ────────────────────────────────────────────────────────────────

// 本地调试：.NET 进程跑在宿主机，需通过 localhost 映射端口访问 Docker 内的 Alloy
// 使用 HTTP 协议（端口 24318），避免 gRPC 在宿主机上的连接问题
var otelEndpoint = "http://localhost:24318";
var otelProtocol = "http/protobuf";

builder.AddProject<Projects.ST_MS_Test_Api>("st-ms-test-api")
	.WithReference(testDb, "Default")
	.WithReference(redis)
	.WithReference(rabbitMq)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
	.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", otelProtocol)
	.WaitFor(testDb)
	.WaitFor(redis)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_MS_FileUpload_Api>("st-ms-fileupload-api")
	.WithReference(fileUploadDb, "Default")
	.WithReference(redis)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
	.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", otelProtocol)
	.WaitFor(fileUploadDb)
	.WaitFor(redis);

builder.AddProject<Projects.ST_MS_Identity_Api>("st-ms-identity-api")
	.WithReference(identityDb, "Default")
	.WithReference(redis)
	.WithReference(rabbitMq)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
	.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", otelProtocol)
	.WaitFor(identityDb)
	.WaitFor(redis)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_MS_OperationLog_Api>("st-ms-operationlog-api")
	.WithReference(operationLogDb, "Default")
	.WithReference(redis)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
	.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", otelProtocol)
	.WaitFor(operationLogDb)
	.WaitFor(redis);

builder.AddProject<Projects.ST_MS_OperationLog_Consumer>("st-ms-operationlog-consumer")
	.WithReference(operationLogDb, "Default")
	.WithReference(rabbitMq)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
	.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", otelProtocol)
	.WaitFor(operationLogDb)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_MS_Inventory_Api>("st-ms-inventory-api")
	.WithReference(inventoryDb, "Default")
	.WithReference(redis)
	.WithReference(rabbitMq)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
	.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", otelProtocol)
	.WaitFor(inventoryDb)
	.WaitFor(redis)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_MS_Payment_Api>("st-ms-payment-api")
	.WithReference(paymentDb, "Default")
	.WithReference(redis)
	.WithReference(rabbitMq)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
	.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", otelProtocol)
	.WaitFor(paymentDb)
	.WaitFor(redis)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_MS_Order_Api>("st-ms-order-api")
	.WithReference(orderDb, "Default")
	.WithReference(redis)
	.WithReference(rabbitMq)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
	.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", otelProtocol)
	.WaitFor(orderDb)
	.WaitFor(redis)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_Gateway>("st-gateway")
	.WithReference(redis)
	.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelEndpoint)
	.WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", otelProtocol)
	.WaitFor(redis);

builder.Build().Run();

