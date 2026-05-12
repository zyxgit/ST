using Microsoft.EntityFrameworkCore;
using ST.Shared.Exceptions;

namespace ST.MS.Identity.Domain.Aggregates.UserAggregate.ValueObject;

[Owned]
public record Password
{
	protected Password() { }   // 给 EF 用

	public Password(string hash, string salt)
	{
		if (string.IsNullOrWhiteSpace(hash))
			throw new BusinessException("密码不能为空");

		if (string.IsNullOrWhiteSpace(salt))
			throw new BusinessException("盐不能为空");

		Hash = hash;
		Salt = salt;
	}

	public string Hash { get; private set; } = null!;
	public string Salt { get; private set; } = null!;
}
