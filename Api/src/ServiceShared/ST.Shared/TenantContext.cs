namespace ST.Shared;

/// <summary>
/// 租户上下文持有器，基于 AsyncLocal 实现请求级别的租户 ID 流转。
/// 供 EF Core 全局查询过滤器和 SaveChanges 自动填充使用。
/// </summary>
public static class TenantContext
{
	private static readonly AsyncLocal<Guid?> _currentTenantId = new();

	/// <summary>
	/// 当前请求的租户 ID（null 表示未指定租户，不过滤）
	/// </summary>
	public static Guid? CurrentTenantId
	{
		get => _currentTenantId.Value;
		set => _currentTenantId.Value = value;
	}

	/// <summary>
	/// 在指定租户上下文中执行操作
	/// </summary>
	public static IDisposable BeginScope(Guid? tenantId)
	{
		var previous = _currentTenantId.Value;
		_currentTenantId.Value = tenantId;
		return new TenantScope(previous);
	}

	private sealed class TenantScope : IDisposable
	{
		private readonly Guid? _previous;
		private bool _disposed;

		public TenantScope(Guid? previous) => _previous = previous;

		public void Dispose()
		{
			if (!_disposed)
			{
				_currentTenantId.Value = _previous;
				_disposed = true;
			}
		}
	}
}
