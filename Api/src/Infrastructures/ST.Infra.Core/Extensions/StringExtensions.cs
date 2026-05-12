using System.Text.Json;

namespace ST.Infra.Core.Extensions;

public static class StringExtensions
{
	/// <summary>
	/// 检查指定的字符串是否为 null 或空字符串
	/// </summary>
	/// <param name="value">要检查的字符串值</param>
	/// <returns>如果字符串为 null 或空字符串则返回 true，否则返回 false</returns>
	public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

	/// <summary>
	/// 检查指定的字符串是否不为 null 且不为空字符串
	/// </summary>
	/// <param name="value">要检查的字符串值</param>
	/// <returns>如果字符串不为 null 且不为空字符串则返回 true，否则返回 false</returns>
	public static bool IsNotNullOrEmpty(this string? value) => !string.IsNullOrEmpty(value);

	/// <summary>
	/// 检查指定的字符串是否为 null、空字符串或仅由空白字符组成
	/// </summary>
	/// <param name="value">要检查的字符串值</param>
	/// <returns>如果字符串为 null、空字符串或仅由空白字符组成则返回 true，否则返回 false</returns>
	public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

	/// <summary>
	/// 检查字符串是否不为null、空或仅由空白字符组成
	/// </summary>
	/// <param name="value">要检查的字符串值</param>
	/// <returns>如果字符串不为null、空或仅由空白字符组成则返回true，否则返回false</returns>
	public static bool IsNotNullOrWhiteSpace(this string? value) => !string.IsNullOrWhiteSpace(value);

	/// <summary>
	/// 判断字符串是否符合指定的通配符模式。
	/// 支持的通配符包括：
	/// ? 匹配任意单个字符，
	/// * 匹配零个或多个字符，
	/// # 匹配一个数字字符，
	/// [abc] 匹配括号内任意一个字符，
	/// [!abc] 匹配不在括号内的任意一个字符。
	/// </summary>
	/// <param name="value">要匹配的字符串。</param>
	/// <param name="pattern">通配符模式。</param>
	/// <returns>如果字符串匹配模式，则返回 true；否则返回 false。</returns>
	public static bool IsLike([NotNull] this string value, string pattern)
	{
		// 将模式转换为正则表达式，并使用 ^ 和 $ 匹配整个字符串
		var regexPattern = "^" + Regex.Escape(pattern) + "$";

		// 替换通配符为对应的正则表达式语法
		regexPattern = regexPattern.Replace(@"\[!", "[^")
			.Replace(@"\[", "[")
			.Replace(@"\]", "]")
			.Replace(@"\?", ".")
			.Replace(@"\*", ".*")
			.Replace(@"\#", @"\d");

		return Regex.IsMatch(value, regexPattern);
	}

	/// <summary>
	/// 将字符串重复指定次数并返回结果。
	/// </summary>
	/// <param name="value">要重复的字符串。</param>
	/// <param name="repeatCount">重复次数。</param>
	/// <returns>重复后的字符串。</returns>
	public static string Repeat([NotNull] this string value, int repeatCount)
	{
		if (value.Length == 1)
		{
			return new string(value[0], repeatCount);
		}

		var sb = new StringBuilder(repeatCount * value.Length);
		while (repeatCount-- > 0)
		{
			sb.Append(value);
		}

		return sb.ToString();
	}

	/// <summary>
	/// 反转字符串中的字符顺序。
	/// </summary>
	/// <param name="value">要反转的字符串。</param>
	/// <returns>反转后的字符串。</returns>
	public static string Reverse([NotNull] this string value)
	{
		if (value.Length <= 1)
		{
			return value;
		}

		var chars = value.ToCharArray();
		Array.Reverse(chars);
		return new string(chars);
	}

	/// <summary>
	/// 使用指定编码将字符串转换为字节数组。
	/// </summary>
	/// <param name="str">要转换的字符串。</param>
	/// <param name="encoding">用于转换的编码方式。</param>
	/// <returns>表示字符串的字节数组。</returns>
	public static byte[] GetBytes([NotNull] this string str, Encoding encoding) => encoding.GetBytes(str);

	/// <summary>
	/// 将字符串转换为枚举类型。
	/// </summary>
	/// <typeparam name="T">目标枚举类型。</typeparam>
	/// <param name="value">要转换的字符串。</param>
	/// <returns>转换后的枚举值。</returns>
	public static T ToEnum<T>([NotNull] this string value) => (T)Enum.Parse(typeof(T), value);

	/// <summary>
	/// 比较两个字符串是否相等，忽略大小写。
	/// </summary>
	/// <param name="s1">第一个字符串。</param>
	/// <param name="s2">第二个字符串。</param>
	/// <returns>如果两个字符串相等（忽略大小写），则返回 true；否则返回 false。</returns>
	public static bool EqualsIgnoreCase(this string s1, string s2) => string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// 尝试将字符串转换为长整型数值。
	/// </summary>
	/// <param name="value">要转换的字符串。</param>
	/// <returns>如果转换成功，返回对应的长整型数值；否则返回 null。</returns>
	public static long? ToLong(this string value)
	{
		var status = long.TryParse(value, out var result);

		if (status)
		{
			return result;
		}
		else
		{
			return null;
		}
	}

	/// <summary>
	/// 将十六进制字符串转换为字节数组。
	/// </summary>
	/// <param name="hex">表示十六进制数据的字符串。</param>
	/// <returns>转换后的字节数组。</returns>
	public static byte[] ToBytes(this string hex)
	{
		if (hex.Length == 0)
		{
			return [0];
		}
		if (hex.Length % 2 == 1)
		{
			hex = "0" + hex;
		}
		var result = new byte[hex.Length / 2];
		for (var i = 0; i < hex.Length / 2; i++)
		{
			result[i] = byte.Parse(hex.Substring(2 * i, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
		}
		return result;
	}

	/// <summary>
	/// 将字符串转换为句子形式
	/// </summary>
	/// <param name="str"></param>
	/// <param name="useCurrentCulture"></param>
	/// <returns></returns>
	public static string ToSentenceCase(this string str, bool useCurrentCulture = false)
	{
		if (string.IsNullOrWhiteSpace(str))
		{
			return str;
		}

		return useCurrentCulture
			? Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]))
			: Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLowerInvariant(m.Value[1]));
	}

	/// <summary>
	/// 将字符串转换为驼峰式
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static string ToCamelCase(this string str)
	{
		if (string.IsNullOrWhiteSpace(str))
		{
			return str;
		}
		return JsonNamingPolicy.CamelCase.ConvertName(str);
	}

	/// <summary>
	/// 将字符串转换为蛇形命名
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static string ToSnakeCase(this string str)
	{
		if (str.IsNullOrEmpty())
			return str;
		return JsonNamingPolicy.SnakeCaseLower.ConvertName(str);
	}

	/// <summary>
	/// 从字符串中移除指定的多个子串（支持忽略大小写，可选）
	/// </summary>
	/// <param name="input">原始字符串</param>
	/// <param name="ignoreCase">是否忽略大小写（true = 忽略）</param>
	/// <param name="toRemove">要移除的子串列表（会自动按长度降序排序）</param>
	/// <returns>处理后的字符串</returns>
	public static string RemoveSubstrings(this string input, bool ignoreCase = false, params string[] toRemove)
	{
		if (string.IsNullOrEmpty(input) || toRemove == null || toRemove.Length == 0)
			return input;

		// 按长度降序排序（先匹配长的子串，避免部分匹配问题）
		Array.Sort(toRemove, (a, b) => b.Length.CompareTo(a.Length));

		// 根据 ignoreCase 选择比较器
		var comparer = ignoreCase
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;

		// 如果忽略大小写，我们统一转成小写来比较（避免多次创建忽略大小写的 Span 比较器开销）
		string workingInput = ignoreCase ? input.ToLowerInvariant() : input;
		var sb = new StringBuilder(input.Length);

		int i = 0;
		while (i < workingInput.Length)
		{
			bool matched = false;

			foreach (var substr in toRemove)
			{
				if (substr.Length == 0) continue;

				int substrLen = substr.Length;
				if (i + substrLen > workingInput.Length) continue;

				// 取当前窗口进行比较
				string window = workingInput.Substring(i, substrLen);

				if (comparer.Equals(window, substr))
				{
					// 匹配成功，跳过这段（注意：跳过的是原始 input 的对应长度）
					i += substrLen;
					matched = true;
					break;
				}
			}

			if (!matched)
			{
				// 未匹配，追加原始 input 的字符（保持原大小写）
				sb.Append(input[i]);
				i++;
			}
		}

		return sb.ToString();
	}
}
