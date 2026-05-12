namespace ST.Infra.Core.Extensions;

public static class IntExtensions
{

	/// <summary>
	/// 判断整数是否为偶数
	/// </summary>
	/// <param name="value">要判断的整数值</param>
	/// <returns>如果是偶数返回true，否则返回false</returns>
	public static bool IsEven(this int value)
	{
		return value % 2 == 0;
	}

	/// <summary>
	/// 判断整数是否为奇数
	/// </summary>
	/// <param name="value">要判断的整数值</param>
	/// <returns>如果是奇数返回true，否则返回false</returns>
	public static bool IsOdd(this int value)
	{
		return value % 2 != 0;
	}

	/// <summary>
	/// 判断整数是否为质数
	/// </summary>
	/// <param name="value">要判断的整数值</param>
	/// <returns>如果是质数返回true，否则返回false</returns>
	public static bool IsPrime(this int value)
	{
		// 处理特殊情况：1和2都是质数
		if (value == 1 || value == 2)
		{
			return true;
		}
		// 偶数（除了2）都不是质数
		if (value % 2 == 0)
		{
			return false;
		}
		// 只需要检查到平方根即可，减少计算量
		var sqrt = (int)Math.Sqrt(value);
		// 从3开始只检查奇数因子
		for (var t = 3; t <= sqrt; t += 2)
		{
			if (value % t == 0)
			{
				return false;
			}
		}
		return true;
	}
}
