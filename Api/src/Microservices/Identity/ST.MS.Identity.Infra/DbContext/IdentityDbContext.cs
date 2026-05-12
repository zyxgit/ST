using ST.Infra.EntityFramework.Npgsql.DbContextBase;
using ST.MS.Identity.Domain.Aggregates.PermissionAggregate;
using ST.MS.Identity.Domain.Aggregates.RoleAggregate;
using ST.MS.Identity.Domain.Aggregates.UserAggregate;

namespace ST.MS.Identity.Infra.DbContext;

public class IdentityDbContext : NpgsqlEfDbContextBase
{
	public DbSet<Permission> Permissions { get; set; }

	public DbSet<Role> Role { get; set; }

	public DbSet<User> Users { get; set; }

	public DbSet<RefreshToken> RefreshTokens { get; set; }

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
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);
	}
}
