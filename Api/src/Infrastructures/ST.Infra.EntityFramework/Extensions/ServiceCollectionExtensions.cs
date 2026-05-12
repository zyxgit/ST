using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.UnitOfWork;
using ST.Infra.Repository.Interface;

namespace ST.Infra.EntityFramework.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddEfInfrastructure<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
	{
		services.AddScoped<IUnitOfWork, EfUnitOfWork>();
		services.AddScoped<DbContext, TDbContext>();
		//services.AddDbContextFactory()
		return services;
	}
}
