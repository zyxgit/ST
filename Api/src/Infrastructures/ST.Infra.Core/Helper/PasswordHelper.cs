using System.Security.Cryptography;

namespace ST.Infra.Core.Helper;

public class PasswordHelper
{
	/// <summary>
	/// 密码加密
	/// </summary>
	/// <param name="password"></param>
	/// <param name="salt"></param>
	/// <returns></returns>
	public static string HashPassword(string password, string salt)
	{
		var passwordBytes = Encoding.UTF8.GetBytes(password);
		var saltBytes = Convert.FromBase64String(salt);

		var combinedBytes = new byte[passwordBytes.Length + saltBytes.Length];
		Array.Copy(passwordBytes, 0, combinedBytes, 0, passwordBytes.Length);
		Array.Copy(saltBytes, 0, combinedBytes, passwordBytes.Length, saltBytes.Length);
		var hash = SHA256.HashData(combinedBytes);
		return Convert.ToBase64String(hash);
	}

	/// <summary>
	/// 校验密码是否正确
	/// </summary>
	/// <param name="password"></param>
	/// <param name="passwordHash"></param>
	/// <param name="salt"></param>
	/// <returns></returns>
	public static bool VerifyPassword(string password, string passwordHash, string salt)
	{
		return HashPassword(password, salt) == passwordHash;
	}


	/// <summary>
	/// 生成一个16 字节的盐值
	/// </summary>
	/// <returns></returns>
	public static byte[] GenerateSalt(int length = 16)
	{
		return RandomNumberGenerator.GetBytes(length);
	}

	/// <summary>
	/// 生成盐值
	/// </summary>
	/// <returns></returns>
	public static string GenerateSaltBase64()
	{
		var salt = Convert.ToBase64String(GenerateSalt());
		return salt;
	}
}
