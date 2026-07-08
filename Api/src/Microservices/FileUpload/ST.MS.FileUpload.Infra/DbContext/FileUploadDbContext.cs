using ST.Infra.EntityFramework.Npgsql.DbContextBase;
using ST.MS.FileUpload.Domain.Entities;

namespace ST.MS.FileUpload.Infra.DbContext;

public sealed class FileUploadDbContext : NpgsqlEfDbContextBase
{
	public DbSet<FileEntity> Files => Set<FileEntity>();
	public DbSet<FileUploadSession> UploadSessions => Set<FileUploadSession>();
	public DbSet<FileUploadChunk> UploadChunks => Set<FileUploadChunk>();

	public FileUploadDbContext(DbContextOptions options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// FileEntity 配置
		modelBuilder.Entity<FileEntity>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.FileName).HasMaxLength(500);
			entity.Property(e => e.FilePath).HasMaxLength(1000);
			entity.Property(e => e.ContentType).HasMaxLength(200);
			entity.Property(e => e.Extension).HasMaxLength(20);
			entity.Property(e => e.UploaderName).HasMaxLength(100);
			entity.Property(e => e.FileHash).HasMaxLength(128);
			entity.HasIndex(e => e.FileHash);
		});

		// FileUploadSession 配置
		modelBuilder.Entity<FileUploadSession>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.FileName).HasMaxLength(500);
			entity.Property(e => e.FileHash).HasMaxLength(128);
			entity.Property(e => e.CreatorName).HasMaxLength(100);
			entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
			entity.HasIndex(e => e.Status);
			entity.HasIndex(e => e.ExpiresAtUtc);
			entity.HasIndex(e => e.FileHash);
			entity.HasIndex(e => new { e.CreatedBy, e.Status });
		});

		// FileUploadChunk 配置
		modelBuilder.Entity<FileUploadChunk>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.UploadId).HasColumnName("session_id");
			entity.Property(e => e.ChunkHash).HasMaxLength(128);
			entity.Property(e => e.StoragePath).HasMaxLength(1000);
			entity.HasIndex(e => new { e.UploadId, e.ChunkIndex }).IsUnique();
			entity.HasOne(e => e.Session)
				.WithMany(s => s.Chunks)
				.HasForeignKey(e => e.UploadId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		base.OnModelCreating(modelBuilder);
	}
}
