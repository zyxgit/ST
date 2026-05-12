using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using ST.Infra.Redis.Provider;

namespace ST.Infra.Redis.Cache;

public class RedisCacheManager : IRedisCacheManager
{
	private readonly IRedisClient _redisConnectionFactory;
	private readonly ILogger<RedisCacheManager> _logger;

	private IDatabase _db => GetDatabase();
	private IConnectionMultiplexer _connection => _redisConnectionFactory.GetConnection();

	/// <summary>
	/// 全局统一的默认序列化配置
	/// </summary>
	private static readonly JsonSerializerOptions _defaultJsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true,
		Encoder = JavaScriptEncoder.Create(new TextEncoderSettings(UnicodeRanges.All)),
		ReadCommentHandling = JsonCommentHandling.Skip
	};

	public RedisCacheManager(IRedisClient redisConnectionFactory,
						  ILogger<RedisCacheManager> logger)
	{
		_redisConnectionFactory = redisConnectionFactory;
		_logger = logger;
	}


	public IDatabase GetDatabase() => _connection.GetDatabase();

	#region 基础操作

	public async Task<bool> ExistsAsync(string key)
	{
		return await _db.KeyExistsAsync(key);
	}

	public async Task<bool> RemoveAsync(string key)
	{
		return await _db.KeyDeleteAsync(key);
	}

	public async Task<bool> RemoveByPatternAsync(string pattern)
	{
		if (string.IsNullOrWhiteSpace(pattern))
		{
			return false;
		}

		var endpoints = _connection.GetEndPoints();
		var server = _connection.GetServer(endpoints.First());

		var deletedAny = false;
		var batch = new List<RedisKey>(capacity: 500);

		foreach (var key in server.Keys(pattern: $"*{pattern}*"))
		{
			batch.Add(key);

			if (batch.Count >= 500)
			{
				deletedAny = true;
				await _db.KeyDeleteAsync(batch.ToArray());
				batch.Clear();
			}
		}

		if (batch.Count > 0)
		{
			deletedAny = true;
			await _db.KeyDeleteAsync(batch.ToArray());
		}

		return deletedAny;
	}

	public async Task ClearAsync()
	{
		var endpoints = _connection.GetEndPoints();
		var server = _connection.GetServer(endpoints.First());

		var batch = new List<RedisKey>(capacity: 500);
		await foreach (var key in server.KeysAsync())
		{
			batch.Add(key);

			if (batch.Count >= 500)
			{
				await _db.KeyDeleteAsync(batch.ToArray());
				batch.Clear();
			}
		}

		if (batch.Count > 0)
		{
			await _db.KeyDeleteAsync(batch.ToArray());
		}
	}
	#endregion

	#region 字符串

	public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always) => await _db.StringSetAsync(key, value, expiry, when);

	public async Task<string?> GetStringAsync(string key) => await _db.StringGetAsync(key);

	#endregion

	#region 对象

	public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, JsonSerializerOptions? options = null)
	{
		var json = JsonSerializer.Serialize(value, options ?? _defaultJsonOptions);
		return await _db.StringSetAsync(key, json, expiry, when);
	}

	public async Task<T?> GetAsync<T>(string key, JsonSerializerOptions? options = null)
	{
		var value = await _db.StringGetAsync(key);
		if (value.IsNullOrEmpty) return default;
		return JsonSerializer.Deserialize<T>(value.ToString(), options ?? _defaultJsonOptions);
	}

	#endregion

	#region 哈希

	public async Task HashSetAsync<T>(string key, string field, T value, JsonSerializerOptions? options = null)
	{
		var json = JsonSerializer.Serialize(value, options ?? _defaultJsonOptions);
		await _db.HashSetAsync(key, field, json);
	}

	public async Task<T?> HashGetAsync<T>(string key, string field, JsonSerializerOptions? options = null)
	{
		var value = await _db.HashGetAsync(key, field);
		if (value.IsNullOrEmpty) return default;
		return JsonSerializer.Deserialize<T>(value.ToString(), options ?? _defaultJsonOptions);
	}

	public async Task<Dictionary<string, T>> HashGetAllAsync<T>(string key, JsonSerializerOptions? options = null)
	{
		var entries = await _db.HashGetAllAsync(key);
		return entries.ToDictionary(
			x => x.Name.ToString(),
			x => JsonSerializer.Deserialize<T>(x.Value.ToString(), options ?? _defaultJsonOptions)!
		);
	}

	public async Task<long> ListLeftPushAsync<T>(string key, T value, JsonSerializerOptions? options = null)
	{
		var json = JsonSerializer.Serialize(value, options ?? _defaultJsonOptions);
		return await _db.ListLeftPushAsync(key, json);
	}

	public async Task<T?> ListLeftPopAsync<T>(string key, JsonSerializerOptions? options = null)
	{
		var value = await _db.ListLeftPopAsync(key);
		if (value.IsNullOrEmpty) return default;
		return JsonSerializer.Deserialize<T>(value.ToString(), options ?? _defaultJsonOptions);
	}

	public async Task<long> ListLengthAsync(string key) => await _db.ListLengthAsync(key);

	public async Task<List<T>> ListRangeAsync<T>(string key, long start = 0, long stop = -1, JsonSerializerOptions? options = null)
	{
		var values = await _db.ListRangeAsync(key, start, stop);
		return values
			.Where(x => !x.IsNullOrEmpty)
			.Select(x => JsonSerializer.Deserialize<T>(x.ToString(), options ?? _defaultJsonOptions)!)
			.ToList();
	}

	#endregion
}
