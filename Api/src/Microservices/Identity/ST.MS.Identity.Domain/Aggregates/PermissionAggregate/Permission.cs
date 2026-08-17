using Microsoft.EntityFrameworkCore;
using ST.Infra.Repository.Entities;
using ST.MS.Identity.Domain.Enums;
using ST.Shared.Exceptions;

namespace ST.MS.Identity.Domain.Aggregates.PermissionAggregate;

/// <summary>
/// 权限表
/// </summary>
[Index(nameof(Code), IsUnique = true)]
public class Permission : AggregateRoot, ISoftDelete
{
	protected Permission() { } // 👈 给 EF 用

	public Permission(Guid? pId, string code, string name, PermissionType type, string? path)
	{
		PId = pId;
		Code = code;
		Name = name;
		Type = type;
		Path = path;
		ValidateState();
	}

	/// <summary>
	/// 父级权限Id
	/// </summary>
	public Guid? PId { get; private set; }

	/// <summary>
	/// 权限编码
	/// </summary>
	public string Code { get; private set; } = null!; // user:create

	/// <summary>
	/// 权限名称
	/// </summary>
	public string Name { get; private set; } = null!;

	/// <summary>
	/// 权限类型
	/// </summary>
	public PermissionType Type { get; private set; }     // Catalogue / Menu / Button

	/// <summary>
	/// 路由
	/// </summary>
	public string? Path { get; private set; }

	/// <summary>
	/// 图标
	/// </summary>
	public string? MenuIcon { get; private set; }

	/// <summary>
	/// 组件路径
	/// </summary>
	public string? Component { get; private set; }

	/// <summary>
	/// 是否外链
	/// </summary>
	public bool IsLink { get; private set; }

	/// <summary>
	/// 是否缓存
	/// </summary>
	public bool KeepAlive { get; private set; }

	/// <summary>
	/// 是否隐藏
	/// </summary>
	public bool IsHide { get; private set; }

	/// <summary>
	/// 排序号（越小越靠前）
	/// </summary>
	public int Sort { get; private set; }

	/// <summary>
	/// 是否删除
	/// </summary>
	public bool IsDeleted { get; set; }

	public void UpdateBasicInfo(
		Guid? parentId,
		string code,
		string name,
		PermissionType type,
		string? path)
	{
		if (parentId == Id)
			throw new BusinessException("不能设置自己为父级");

		PId = parentId;
		Code = code;
		Name = name;
		Type = type;
		Path = path;
		ValidateState();
	}

	public void UpdatePresentation(
		string? menuIcon,
		string? component,
		bool isLink,
		bool keepAlive,
		bool isHide,
		int sort = 0)
	{
		MenuIcon = string.IsNullOrWhiteSpace(menuIcon) ? null : menuIcon.Trim();
		Component = string.IsNullOrWhiteSpace(component) ? null : component.Trim();
		IsLink = isLink;
		KeepAlive = keepAlive;
		IsHide = isHide;
		Sort = sort;
	}

	public void SoftDelete() => IsDeleted = true;

	private void ValidateState()
	{
		if (string.IsNullOrWhiteSpace(Code))
			throw new DomainException("权限编码不能为空");

		if (string.IsNullOrWhiteSpace(Name))
			throw new DomainException("权限名称不能为空");

		if (Type == PermissionType.Button && !string.IsNullOrEmpty(Path))
			throw new DomainException("按钮类型不能有路由");

		if (Type != PermissionType.Button && string.IsNullOrEmpty(Path))
			throw new DomainException("菜单/目录必须有路由");
	}
}
