using System.Text;

Console.OutputEncoding = Encoding.UTF8;
var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env");

IResourceBuilder<RedisResource> redis;
IResourceBuilder<PostgresServerResource> postgres;
IResourceBuilder<RabbitMQServerResource> rabbitMq;

var password = builder.AddParameter("password", "pw123456");

var pguser = builder.AddParameter("pguser", "postgres");

var rabbitUser = builder.AddParameter("rabbitUser", "guest");
var rabbitPassword = builder.AddParameter("rabbitPassword", "guest");

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

rabbitMq = builder.AddRabbitMQ("rabbitmq", rabbitUser, rabbitPassword, port: 5672)
	.WithDataVolume()
	.WithManagementPlugin(port: 15672)
	.WithLifetime(ContainerLifetime.Persistent);


builder.AddProject<Projects.ST_MS_Test_Api>("st-ms-test-api")
	.WaitFor(redis)
	.WaitFor(postgres)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_MS_FileUpload_Api>("st-ms-fileupload-api")
	.WaitFor(redis)
	.WaitFor(postgres)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_MS_Identity_Api>("st-ms-identity-api")
	.WaitFor(redis)
	.WaitFor(postgres)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_MS_OperationLog_Api>("st-ms-operationlog-api")
	.WaitFor(postgres);

builder.AddProject<Projects.ST_MS_OperationLog_Consumer>("st-ms-operationlog-consumer")
	.WaitFor(postgres)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_Gateway>("st-gateway");

builder.Build().Run();

