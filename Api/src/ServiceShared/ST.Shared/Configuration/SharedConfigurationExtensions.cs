using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace ST.Shared.Configuration;

public static class SharedConfigurationExtensions
{
	private const string SharedConfigResourceName = "ST.Shared.Config.appsettings.Shared.json";

	public static IConfigurationBuilder AddStSharedDefaults(this IConfigurationBuilder configurationBuilder)
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SharedConfigResourceName);
		if (stream is null)
		{
			return configurationBuilder;
		}

		return configurationBuilder.AddJsonStream(stream);
	}
}
