using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ST.Shared.Module;

public interface ISharedModule
{
	Assembly Assembly { get; }

	void ConfigureServices(IServiceCollection services);

	void Configure(IApplicationBuilder app);
}
