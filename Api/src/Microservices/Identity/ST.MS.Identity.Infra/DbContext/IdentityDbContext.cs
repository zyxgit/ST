using ST.Infra.EntityFramework.Npgsql.DbContextBase;
using ST.MS.Identity.Domain.Aggregates.PermissionAggregate;
using ST.MS.Identity.Domain.Aggregates.RoleAggregate;
using ST.MS.Identity.Domain.Aggregates.TenantAggregate;
using ST.MS.Identity.Domain.Aggregates.UserAggregate;

namespace ST.MS.Identity.Infra.DbContext;

public class IdentityDbContext : NpgsqlEfDbContextBase
{
	public DbSet<Permission> Permissions { get; set; }

	public DbSet<Role> Role { get; set; }

	public DbSet<User> Users { get; set; }

	public DbSet<RefreshToken> RefreshTokens { get; set; }

	public DbSet<Tenant> Tenants { get; set; }

	public DbSet<TenantUser> TenantUsers { get; set; }

	public DbSet<TenantQuota> TenantQuotas { get; set; }

	public IdentityDbContext(DbContextOptions options) : base(options)
	{

	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyCommentsFromAttributeOrXml(typeof(User).Assembly);

		modelBuilder.Entity<UserRole>().HasKey(c => new { c.UserId, c.RoleId });
		modelBuilder.Entity<RolePermission>().HasKey(c => new { c.RoleId, c.PermissionId });

		modelBuilder.Entity<RefreshToken>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => x.TokenHash).IsUnique();
			entity.Property(x => x.TokenHash).HasMaxLength(128);
		});

		// 租户
		modelBuilder.Entity<Tenant>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => x.Code).IsUnique();
			entity.Property(x => x.Code).HasMaxLength(64);
			entity.Property(x => x.Name).HasMaxLength(200);
			entity.Property(x => x.PackageId).HasMaxLength(64);
		});

		// 租户用户关联（复合主键）
		modelBuilder.Entity<TenantUser>().HasKey(c => new { c.TenantId, c.UserId });
		modelBuilder.Entity<TenantUser>(entity =>
		{
			entity.Property(x => x.RoleInTenant).HasMaxLength(32);
		});

		// 租户配额
		modelBuilder.Entity<TenantQuota>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => x.TenantId).IsUnique();
		});
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);
	}
}
