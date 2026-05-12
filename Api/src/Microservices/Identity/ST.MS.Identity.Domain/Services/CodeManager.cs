using System.Security.Cryptography;
using ST.Shared.Exceptions;


namespace ST.MS.Identity.Domain.Services;

public class CodeManager : IDomainService
{
	public string GenerateCode(int length = 6)
	{
		var max = (int)Math.Pow(10, length);
		return RandomNumberGenerator
			.GetInt32(0, max)
			.ToString($"D{length}");
	}

	public void Verify(string? dbCode, string inputCode)
	{
		if (string.IsNullOrEmpty(dbCode))
			throw new BusinessException("验证码已过期");

		if (dbCode != inputCode)
			throw new BusinessException("验证码错误");
	}
}
