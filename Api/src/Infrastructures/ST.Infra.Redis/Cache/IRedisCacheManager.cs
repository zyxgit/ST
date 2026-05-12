using System.Text.Json;

namespace ST.Infra.Redis.Cache;

public interface IRedisCacheManager
{
	/// <summary>
	/// 获取Redis数据库操作实例，用于直接访问底层Redis数据库。
	/// </summary>
	/// <returns>Redis数据库操作接口实例</returns>
	IDatabase GetDatabase();

	/// <summary>
	/// 检查指定键是否存在
	/// </summary>
	/// <param name="key">要检查的缓存键</param>
	/// <returns>如果键存在则返回true，否则返回false</returns>
	Task<bool> ExistsAsync(string key);

	/// <summary>
	/// 删除指定键的缓存项
	/// </summary>
	/// <param name="key">要删除的缓存键</param>
	/// <returns>如果删除成功则返回true，否则返回false</returns>
	Task<bool> RemoveAsync(string key);

	/// <summary>
	/// 根据模式匹配删除多个缓存项
	/// </summary>
	/// <param name="pattern">匹配模式，支持通配符（如"prefix_*"）</param>
	/// <returns>删除成功则返回true，否则返回false</returns>
	Task<bool> RemoveByPatternAsync(string pattern);

	/// <summary>
	/// 清空当前数据库中的所有缓存项
	/// </summary>
	/// <returns>异步操作任务</returns>
	Task ClearAsync();

	/// <summary>
	/// 设置字符串类型的缓存项
	/// </summary>
	/// <param name="key">缓存键</param>
	/// <param name="value">字符串值</param>
	/// <param name="expiry">过期时间，null表示永不过期</param>
	/// <returns>如果设置成功则返回true</returns>
	Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always);

	/// <summary>
	/// 获取字符串类型的缓存项
	/// </summary>
	/// <param name="key">缓存键</param>
	/// <returns>缓存的字符串值，如果不存在则返回null</returns>
	Task<string?> GetStringAsync(string key);

	/// <summary>
	/// 设置序列化对象类型的缓存项
	/// </summary>
	/// <typeparam name="T">对象类型</typeparam>
	/// <param name="key">缓存键</param>
	/// <param name="value">要缓存的对象</param>
	/// <param name="expiry">过期时间，null表示永不过期</param>
	/// <param name="options">JSON序列化选项，可自定义序列化行为</param>
	/// <returns>如果设置成功则返回true</returns>
	Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, JsonSerializerOptions? options = null);

	/// <summary>
	/// 获取并反序列化指定类型的缓存对象
	/// </summary>
	/// <typeparam name="T">目标对象类型</typeparam>
	/// <param name="key">缓存键</param>
	/// <param name="options">JSON反序列化选项，可自定义反序列化行为</param>
	/// <returns>反序列化后的对象，如果不存在则返回null</returns>
	Task<T?> GetAsync<T>(string key, JsonSerializerOptions? options = null);

	/// <summary>
	/// 在哈希表中设置字段值（序列化对象）
	/// </summary>
	/// <typeparam name="T">值的类型</typeparam>
	/// <param name="key">哈希表键</param>
	/// <param name="field">哈希字段名</param>
	/// <param name="value">要存储的值</param>
	/// <param name="options">JSON序列化选项</param>
	Task HashSetAsync<T>(string key, string field, T value, JsonSerializerOptions? options = null);

	/// <summary>
	/// 从哈希表中获取并反序列化指定字段的值
	/// </summary>
	/// <typeparam name="T">目标对象类型</typeparam>
	/// <param name="key">哈希表键</param>
	/// <param name="field">哈希字段名</param>
	/// <param name="options">JSON反序列化选项</param>
	/// <returns>反序列化后的对象，如果字段不存在则返回null</returns>
	Task<T?> HashGetAsync<T>(string key, string field, JsonSerializerOptions? options = null);

	/// <summary>
	/// 获取哈希表中所有字段及其值的字典
	/// </summary>
	/// <typeparam name="T">值的类型</typeparam>
	/// <param name="key">哈希表键</param>
	/// <param name="options">JSON反序列化选项</param>
	/// <returns>包含所有字段和对应值的字典</returns>
	Task<Dictionary<string, T>> HashGetAllAsync<T>(string key, JsonSerializerOptions? options = null);

	/// <summary>
	/// 将元素推入列表左侧（头部）
	/// </summary>
	/// <typeparam name="T">元素类型</typeparam>
	/// <param name="key">列表键</param>
	/// <param name="value">要添加的元素</param>
	/// <param name="options">JSON序列化选项</param>
	/// <returns>操作后列表的长度</returns>
	Task<long> ListLeftPushAsync<T>(string key, T value, JsonSerializerOptions? options = null);

	/// <summary>
	/// 从列表左侧（头部）弹出一个元素
	/// </summary>
	/// <typeparam name="T">元素类型</typeparam>
	/// <param name="key">列表键</param>
	/// <param name="options">JSON反序列化选项</param>
	/// <returns>弹出的元素，如果列表为空则返回null</returns>
	Task<T?> ListLeftPopAsync<T>(string key, JsonSerializerOptions? options = null);

	/// <summary>
	/// 获取列表的长度
	/// </summary>
	/// <param name="key">列表键</param>
	/// <returns>列表中元素的数量</returns>
	Task<long> ListLengthAsync(string key);

	/// <summary>
	/// 获取列表中指定范围的元素
	/// </summary>
	/// <typeparam name="T">元素类型</typeparam>
	/// <param name="key">列表键</param>
	/// <param name="start">起始索引（默认为0，表示开头）</param>
	/// <param name="stop">结束索引（默认为-1，表示结尾）</param>
	/// <param name="options">JSON反序列化选项</param>
	/// <returns>指定范围内的元素列表</returns>
	Task<List<T>> ListRangeAsync<T>(string key, long start = 0, long stop = -1, JsonSerializerOptions? options = null);
}
