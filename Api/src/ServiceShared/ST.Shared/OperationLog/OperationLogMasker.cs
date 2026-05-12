using System.Text.Json;

namespace ST.Shared.OperationLog;

public static class OperationLogMasker
{
	public static string MaskJson(string json, OperationLogOptions options)
	{
		if (!options.MaskEnabled)
		{
			return json;
		}

		if (string.IsNullOrWhiteSpace(json))
		{
			return json;
		}

		using var doc = JsonDocument.Parse(json);
		using var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream);

		WriteMasked(doc.RootElement, writer, options);
		writer.Flush();

		return System.Text.Encoding.UTF8.GetString(stream.ToArray());
	}

	private static void WriteMasked(JsonElement element, Utf8JsonWriter writer, OperationLogOptions options)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				writer.WriteStartObject();
				foreach (var property in element.EnumerateObject())
				{
					writer.WritePropertyName(property.Name);
					if (IsSensitiveKey(property.Name, options))
					{
						writer.WriteStringValue(options.Mask);
						continue;
					}

					WriteMasked(property.Value, writer, options);
				}
				writer.WriteEndObject();
				return;

			case JsonValueKind.Array:
				writer.WriteStartArray();
				foreach (var item in element.EnumerateArray())
				{
					WriteMasked(item, writer, options);
				}
				writer.WriteEndArray();
				return;

			default:
				element.WriteTo(writer);
				return;
		}
	}

	private static bool IsSensitiveKey(string key, OperationLogOptions options)
	{
		if (options.SensitiveKeys is null || options.SensitiveKeys.Length == 0)
		{
			return false;
		}

		foreach (var sensitiveKey in options.SensitiveKeys)
		{
			if (key.Equals(sensitiveKey, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}

