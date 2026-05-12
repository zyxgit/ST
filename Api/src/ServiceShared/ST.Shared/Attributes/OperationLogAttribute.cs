namespace ST.Shared.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class OperationLogAttribute : Attribute
{
	public OperationLogAttribute(string operationName)
	{
		OperationName = operationName;
	}

	public string OperationName { get; }

	public bool RecordRequest { get; set; } = true;

	public bool RecordResponse { get; set; } = false;

	/// <summary>
	/// 单条请求/响应日志最大字符长度，避免大对象导致性能问题。
	/// </summary>
	public int MaxBodyLength { get; set; } = 8_192;
}

