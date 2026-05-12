namespace ST.Infra.Repository.Entities;

public interface IConcurrency
{
	/// <summary>
	/// 并发控制列
	/// </summary>
	Guid RowVersion { get; set; }
}
