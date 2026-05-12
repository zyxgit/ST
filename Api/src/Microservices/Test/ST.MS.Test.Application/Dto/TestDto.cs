namespace ST.MS.Test.Application.Dto;

public class TestDto
{
	/// <summary>
	/// id
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// 名称
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 备注
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// 手机号
	/// </summary>
	public int Age { get; set; }
}
