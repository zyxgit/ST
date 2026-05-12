using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.MS.Test.Application.Dto;
using ST.MS.Test.Application.Services;
using ST.Shared.WebApi.Controller;

namespace ST.MS.Test.Api.Controllers;

[AllowAnonymous]
public class TestController : AbstractControllerBase
{
	private readonly TestService _testService;
	private readonly ILogger<TestController> _logger;

	public TestController(TestService testService,
		ILogger<TestController> logger,
		ITestService testService1)
	{
		_testService = testService;
		_logger = logger;
	}

	/// <summary>
	/// 测试获取
	/// </summary>
	/// <returns></returns>
	[HttpGet("test")]
	public ActionResult<string> Get()
	{
		return _testService.Test();

		//return Ok(_testService.Test());
	}

	/// <summary>
	/// notContent
	/// </summary>
	/// <returns></returns>
	[HttpGet("error")]
	public ActionResult ErrorTest()
	{
		throw new Exception("发生错误啦");
	}

	/// <summary>
	/// 业务错误
	/// </summary>
	/// <returns></returns>
	[HttpGet("bussiness")]
	public IActionResult BusinessError()
	{
		//return BadRequest("你是憨批蛮");
		throw new BusinessException("你是憨批蛮", errorCode: "RVFGFG_ASDADSA");
	}

	/// <summary>
	/// 记录日志
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	[HttpPost("log/{message}")]
	public async Task<IActionResult> Log(string message)
	{
		_logger.LogDebug(message ?? "");

		return Ok();

	}

	/// <summary>
	/// redis set
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	[HttpPost("redis")]
	public async Task RedisSet(string key, string value)
	{
		await _testService.RedisSet(key, value);
	}

	/// <summary>
	/// redis get
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	[HttpGet("redis/{key}")]
	public async Task<string?> RedisGet(string key)
	{
		var value = await _testService.RedisGet(key);
		return value;
	}

	/// <summary>
	/// uow测试
	/// </summary>
	/// <returns></returns>
	[HttpPost("uowInterceptor")]
	public async Task UowTest()
	{
		//await _testService1.TestUow1111();
	}

	/// <summary>
	/// uow测试
	/// </summary>
	/// <returns></returns>
	[HttpPost("uow")]
	public async Task UowTestBegin()
	{
		await _testService.TestUow();
	}

	/// <summary>
	/// 测试列表
	/// </summary>
	/// <returns></returns>
	[HttpGet("list")]
	public async Task<ActionResult<List<TestDto>>> GetTestList()
	{
		//return await _testService.GetTests();
		return new List<TestDto>();
	}
}
