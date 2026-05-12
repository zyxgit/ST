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

rabbitMq = builder.AddRabbitMQ("rabbitmq", rabbitUser, rabbitPassword, port: 5672)
	.WithDataVolume()
	.WithManagementPlugin(port: 15672)
	.WithLifetime(ContainerLifetime.Persistent);


builder.AddProject<Projects.ST_MS_Test_Api>("st-ms-test-api")
	.WithReference(testDb, "Default")
	.WithReference(redis)
	.WithReference(rabbitMq)
	.WaitFor(testDb)
	.WaitFor(redis)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_MS_FileUpload_Api>("st-ms-fileupload-api")
	.WithReference(fileUploadDb, "Default")
	.WithReference(redis)
	.WaitFor(fileUploadDb)
	.WaitFor(redis);

builder.AddProject<Projects.ST_MS_Identity_Api>("st-ms-identity-api")
	.WithReference(identityDb, "Default")
	.WithReference(redis)
	.WithReference(rabbitMq)
	.WaitFor(identityDb)
	.WaitFor(redis)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_MS_OperationLog_Api>("st-ms-operationlog-api")
	.WithReference(operationLogDb, "Default")
	.WithReference(redis)
	.WaitFor(operationLogDb)
	.WaitFor(redis);

builder.AddProject<Projects.ST_MS_OperationLog_Consumer>("st-ms-operationlog-consumer")
	.WithReference(operationLogDb, "Default")
	.WithReference(rabbitMq)
	.WaitFor(operationLogDb)
	.WaitFor(rabbitMq);

builder.AddProject<Projects.ST_Gateway>("st-gateway");

builder.Build().Run();

