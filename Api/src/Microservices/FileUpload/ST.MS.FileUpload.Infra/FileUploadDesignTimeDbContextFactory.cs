using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.FileUpload.Infra.DbContext;
using ST.Shared.Const;

namespace ST.MS.FileUpload.Infra;

public sealed class FileUploadDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<FileUploadDbContext>
{
	protected override string GetConnectionString(string[] args)
	{
		return Environment.GetEnvironmentVariable(SettingPrefixContants.Database_ConnectionString_Env)
			?? throw new InvalidOperationException($"Design-time database connection string is not configured. Set the '{SettingPrefixContants.Database_ConnectionString_Env}' environment variable.");
	}

	protected override FileUploadDbContext CreateDbContext(DbContextOptions options, string[] args)
	{
		return new FileUploadDbContext(options);
	}
}
