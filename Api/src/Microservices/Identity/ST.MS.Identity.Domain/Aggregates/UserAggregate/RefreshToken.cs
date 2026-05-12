using ST.Shared.Exceptions;

namespace ST.MS.Identity.Domain.Aggregates.UserAggregate;

/// <summary>
/// 刷新 Token（仅保存哈希值，不保存明文）
/// </summary>
public class RefreshToken : IEntity
{
	public Guid Id { get; set; }

	public Guid UserId { get; set; }

	/// <summary>
	/// RefreshToken 的 SHA256(Base64)
	/// </summary>
	public string TokenHash { get; set; } = string.Empty;

	public DateTime CreatedAtUtc { get; set; }

	public DateTime ExpiresAtUtc { get; set; }

	public DateTime? RevokedAtUtc { get; set; }

	public string? CreatedByIp { get; set; }

	public string? RevokedByIp { get; set; }

	public string? ReplacedByTokenHash { get; set; }

	public bool IsRevoked => RevokedAtUtc.HasValue;

	public void Revoke(DateTime revokedAtUtc, string? revokedByIp, string? replacedByTokenHash)
	{
		if (IsRevoked)
		{
			return;
		}

		if (revokedAtUtc.Kind != DateTimeKind.Utc)
		{
			throw new DomainException("撤销时间必须为 UTC");
		}

		RevokedAtUtc = revokedAtUtc;
		RevokedByIp = revokedByIp;
		ReplacedByTokenHash = replacedByTokenHash;
	}
}
