using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ST.Infra.Email.Abstractions;
using ST.Infra.Email.Smtp;

namespace ST.Infra.Email.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInfraEmail(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));

		services.AddTransient<IEmailSender, SmtpEmailSender>();

		return services;
	}
}
