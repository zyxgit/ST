using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ST.Shared.Module;

public class ServiceModule : ISharedModule
{
	public virtual Assembly Assembly => GetType().Assembly;

	// 仅用于 MS.DI（可选）
	public virtual void ConfigureServices(IServiceCollection services)
	{
	}

	public virtual void Configure(IApplicationBuilder app)
	{
	}
}
