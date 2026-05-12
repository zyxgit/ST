namespace ST.Infra.Repository.Entities;

public interface ISoftDelete
{
	/// <summary>
	/// 是否已删除
	/// </summary>
	bool IsDeleted { get; set; }
}
