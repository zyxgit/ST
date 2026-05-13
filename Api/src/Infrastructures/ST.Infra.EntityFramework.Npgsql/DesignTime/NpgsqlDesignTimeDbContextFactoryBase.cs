using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using ST.Infra.EntityFramework.Configuration;

namespace ST.Infra.EntityFramework.Npgsql.DesignTime;

public abstract class NpgsqlDesignTimeDbContextFactoryBase<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    public TDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();

        optionsBuilder.UseNpgsql(GetConnectionString(args));

        // 禁止迁移生成外键（根据项目约定，外键由业务/脚本控制）
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, NoForeignKeySqlGenerator>();

        Configure(optionsBuilder, args);

        return CreateDbContext(optionsBuilder.Options, args);
    }

    protected virtual void Configure(DbContextOptionsBuilder<TDbContext> optionsBuilder, string[] args)
    {
    }

    /// <summary>
    /// 获取连接字符串。默认实现按以下优先级解析：
    ///   appsettings.json → appsettings.Development.json → User Secrets → 环境变量
    /// </summary>
    protected virtual string GetConnectionString(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(typeof(TDbContext).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        return DatabaseConnectionInfoResolver.Resolve(configuration).ConnectionString;
    }

    protected abstract TDbContext CreateDbContext(DbContextOptions options, string[] args);
}
