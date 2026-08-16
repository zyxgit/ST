using ST.Infra.Redis.Cache;
using ST.MS.Test.Domain.Entities;
using ST.Shared.Application.Services;

namespace ST.MS.Test.Application.Services;

public class TestService : AbstractAppService
{
	private readonly IRedisCacheManager _redisManager;


	public TestService(IRedisCacheManager redisManager)
	{
		_redisManager = redisManager;
	}

	public string Test()
	{
		return "TestDependency";
	}

	/// <summary>
	/// Redis测试
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public async Task RedisSet(string key, string value)
	{
		await _redisManager.SetStringAsync(key, value, TimeSpan.FromMinutes(10));
	}

	/// <summary>
	/// Redis测试
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public async Task<string?> RedisGet(string key)
	{
		return await _redisManager.GetStringAsync(key);
	}

	/// <summary>
	/// uow
	/// </summary>
	/// <returns></returns>
	public async Task TestUow1111()
	{
		TestEntity testEntity1 = new("Test1111", "Test1111", 1111);
		TestEntity testEntity2 = new("Test2222", "Test2222", 2222);
	}

	public async Task TestUow()
	{
		TestEntity testEntity = new TestEntity("Test3333", "Test3333", 3333333);

	}

	//public async Task<List<TestDto>> GetTests()
	//{
	//	//var list = await _testRepo.GetListAsync();
	//	//return list.Adapt<List<TestDto>>();
	//}
}
