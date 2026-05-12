using Mapster;

namespace ST.Shared.Application.Mapster;

public class MapsterConfig
{
	public static TypeAdapterConfig Config => GetTypeAdapterConfig();

	static TypeAdapterConfig GetTypeAdapterConfig()
	{
		var config = new TypeAdapterConfig();

		return config;
	}
}
