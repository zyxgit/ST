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
               ?? "Host=localhost;Port=15432;Username=postgres;Password=pw123456;Database=st_fileupload;";
    }

    protected override FileUploadDbContext CreateDbContext(DbContextOptions options, string[] args)
    {
        return new FileUploadDbContext(options);
    }
}
