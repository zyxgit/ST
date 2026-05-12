namespace ST.MS.Test.Domain.Entities;

public class TestEntity : DomainEntity
{
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

	public TestEntity()
	{
	}

	public TestEntity(string name, string description, int age)
	{
		Id = Guid.CreateVersion7();
		Name = name;
		Description = description;
		Age = age;
	}
}
