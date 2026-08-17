using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.DbContextBase;

namespace ST.Infra.EntityFramework.Npgsql.DbContextBase;

public abstract class NpgsqlEfDbContextBase : EfDbContextBase
{
	protected NpgsqlEfDbContextBase(DbContextOptions options) : base(options)
	{
	}
}
