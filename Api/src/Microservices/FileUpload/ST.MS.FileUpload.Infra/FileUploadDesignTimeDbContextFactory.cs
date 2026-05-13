using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.FileUpload.Infra.DbContext;

namespace ST.MS.FileUpload.Infra;

public sealed class FileUploadDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<FileUploadDbContext>
{
    protected override FileUploadDbContext CreateDbContext(DbContextOptions options, string[] args)
    {
        return new FileUploadDbContext(options);
    }
}
