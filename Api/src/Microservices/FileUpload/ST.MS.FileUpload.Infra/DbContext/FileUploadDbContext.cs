using ST.Infra.EntityFramework.Npgsql.DbContextBase;
using ST.MS.FileUpload.Domain.Entities;

namespace ST.MS.FileUpload.Infra.DbContext;

public sealed class FileUploadDbContext : NpgsqlEfDbContextBase
{
    public DbSet<FileEntity> Files => Set<FileEntity>();

    public FileUploadDbContext(DbContextOptions options) : base(options)
    {
    }
}
