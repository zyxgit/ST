namespace ST.MS.Test.Domain.Entities;

public class Permission : AggregateRoot
{
	public string Code { get; private set; } = string.Empty;   // user:create
	public string Name { get; private set; } = string.Empty;
	public int Type { get; private set; }     // API / Menu / Button
	public string? Path { get; private set; }
}
