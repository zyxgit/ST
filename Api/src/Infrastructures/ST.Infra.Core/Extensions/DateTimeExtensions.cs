using System.Diagnostics;
using System.Globalization;

namespace ST.Infra.Core.Extensions;

public static class DateTimeExtensions
{
	/// <summary>
	/// 将 Unix 时间戳转换为本地时间的 DateTime 对象。
	/// </summary>
	/// <param name="timestamp">表示自 1970 年 1 月 1 日以来经过的秒数。</param>
	/// <returns>对应的本地时间 DateTime 对象。</returns>
	public static DateTime ToLocalTime(this long timestamp)
	{
		var dto = DateTimeOffset.FromUnixTimeSeconds(timestamp);
		return dto.ToLocalTime().DateTime;
	}

	/// <summary>
	/// 获取指定年份中的总周数（以周一作为一周开始）。
	/// </summary>
	/// <param name="_">当前 DateTime 实例（未使用）。</param>
	/// <param name="year">要查询的年份。</param>
	/// <returns>该年的总周数。</returns>
	public static int GetWeekAmount(this DateTime _, int year)
	{
		var end = new DateTime(year, 12, 31); //该年最后一天
		var gc = new GregorianCalendar();
		return gc.GetWeekOfYear(end, CalendarWeekRule.FirstDay, DayOfWeek.Monday); //该年星期数
	}

	/// <summary>
	/// 返回给定日期在一年中属于第几周，默认从周日开始计算。
	/// </summary>
	/// <param name="value">输入的日期。</param>
	/// <returns>该日期所在的一年中的周数。</returns>
	public static int WeekOfYear(this in DateTime value)
	{
		var gc = new GregorianCalendar();
		return gc.GetWeekOfYear(value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
	}

	/// <summary>
	/// 根据指定的每周起始日，返回给定日期在一年中属于第几周。
	/// </summary>
	/// <param name="date">输入的日期。</param>
	/// <param name="week">一周的起始日。</param>
	/// <returns>该日期所在的一年中的周数。</returns>
	public static int WeekOfYear(this in DateTime date, DayOfWeek week)
	{
		var gc = new GregorianCalendar();
		return gc.GetWeekOfYear(date, CalendarWeekRule.FirstDay, week);
	}

	/// <summary>
	/// 获取某一年某一自然周的起止日期（周一至周日）。
	/// </summary>
	/// <param name="_">当前 DateTime 实例（未使用）。</param>
	/// <param name="nYear">目标年份。</param>
	/// <param name="nNumWeek">目标周次。</param>
	/// <param name="dtWeekStart">输出参数：该周的第一天。</param>
	/// <param name="dtWeekEnd">输出参数：该周的最后一天。</param>
	public static void GetWeekTime(this DateTime _, int nYear, int nNumWeek, out DateTime dtWeekStart, out DateTime dtWeekEnd)
	{
		var dt = new DateTime(nYear, 1, 1);
		dt += new TimeSpan((nNumWeek - 1) * 7, 0, 0, 0);
		dtWeekStart = dt.AddDays(-(int)dt.DayOfWeek + (int)DayOfWeek.Monday);
		dtWeekEnd = dt.AddDays((int)DayOfWeek.Saturday - (int)dt.DayOfWeek + 1);
	}

	/// <summary>
	/// 获取某一年某一工作周的起止日期（周一至周五）。
	/// </summary>
	/// <param name="_">当前 DateTime 实例（未使用）。</param>
	/// <param name="nYear">目标年份。</param>
	/// <param name="nNumWeek">目标周次。</param>
	/// <param name="dtWeekStart">输出参数：该工作周的第一天。</param>
	/// <param name="dtWeekEnd">输出参数：该工作周的最后一天。</param>
	public static void GetWeekWorkTime(this DateTime _, int nYear, int nNumWeek, out DateTime dtWeekStart, out DateTime dtWeekEnd)
	{
		var dt = new DateTime(nYear, 1, 1);
		dt += new TimeSpan((nNumWeek - 1) * 7, 0, 0, 0);
		dtWeekStart = dt.AddDays(-(int)dt.DayOfWeek + (int)DayOfWeek.Monday);
		dtWeekEnd = dt.AddDays((int)DayOfWeek.Saturday - (int)dt.DayOfWeek + 1).AddDays(-2);
	}

	/// <summary>
	/// 计算相对于当前日期偏移若干天后的日期字符串格式。
	/// </summary>
	/// <param name="value">原始日期。</param>
	/// <param name="relativeday">相对天数。</param>
	/// <returns>格式化后的日期字符串（"yyyy-MM-dd HH:mm:ss"）。</returns>
	public static string GetDateTime(this in DateTime value, int relativeday)
	{
		return value.AddDays(relativeday).ToString("yyyy-MM-dd HH:mm:ss");
	}

	/// <summary>
	/// 获取自 Unix 纪元（1970-01-01 00:00:00 UTC）以来所经过的秒数。
	/// </summary>
	/// <param name="value">输入的时间点。</param>
	/// <returns>经过的秒数。</returns>
	public static double GetTotalSeconds(this in DateTime value) => new DateTimeOffset(value).ToUnixTimeSeconds();

	/// <summary>
	/// 获取自 Unix 纪元（1970-01-01 00:00:00 UTC）以来所经过的毫秒数。
	/// </summary>
	/// <param name="value">输入的时间点。</param>
	/// <returns>经过的毫秒数。</returns>
	public static double GetTotalMilliseconds(this in DateTime value) => new DateTimeOffset(value).ToUnixTimeMilliseconds();

	/// <summary>
	/// 获取自 Unix 纪元（1970-01-01 00:00:00 UTC）以来所经过的微秒数。
	/// </summary>
	/// <param name="value">输入的时间点。</param>
	/// <returns>经过的微秒数。</returns>
	public static long GetTotalMicroseconds(this in DateTime value) => new DateTimeOffset(value).Ticks / 10;

	/// <summary>
	/// 获取自 Unix 纪元（1970-01-01 00:00:00 UTC）以来所经过的纳秒数。
	/// </summary>
	/// <param name="value">输入的时间点。</param>
	/// <returns>经过的纳秒数。</returns>
	public static long GetTotalNanoseconds(this in DateTime value) => new DateTimeOffset(value).Ticks * 100 + Stopwatch.GetTimestamp() % 100;

	/// <summary>
	/// 获取自 Unix 纪元（1970-01-01 00:00:00 UTC）以来所经过的分钟数。
	/// </summary>
	/// <param name="value">输入的时间点。</param>
	/// <returns>经过的分钟数。</returns>
	public static double GetTotalMinutes(this in DateTime value) => new DateTimeOffset(value).Offset.TotalMinutes;

	/// <summary>
	/// 获取自 Unix 纪元（1970-01-01 00:00:00 UTC）以来所经过的小时数。
	/// </summary>
	/// <param name="value">输入的时间点。</param>
	/// <returns>经过的小时数。</returns>
	public static double GetTotalHours(this in DateTime value) => new DateTimeOffset(value).Offset.TotalHours;

	/// <summary>
	/// 获取自 Unix 纪元（1970-01-01 00:00:00 UTC）以来所经过的天数。
	/// </summary>
	/// <param name="value">输入的时间点。</param>
	/// <returns>经过的天数。</returns>
	public static double GetTotalDays(this in DateTime value) => new DateTimeOffset(value).Offset.TotalDays;

	/// <summary>
	/// 获取指定年份的全年总天数。
	/// </summary>
	/// <param name="_">当前 DateTime 实例（未使用）。</param>
	/// <param name="iYear">目标年份。</param>
	/// <returns>该年份的总天数（平年或闰年）。</returns>
	public static int GetDaysOfYear(this DateTime _, int iYear)
	{
		return IsRuYear(iYear) ? 366 : 365;
	}

	/// <summary>
	/// 获取指定日期对应年份的全年总天数。
	/// </summary>
	/// <param name="value">输入的日期。</param>
	/// <returns>该年份的总天数（平年或闰年）。</returns>
	public static int GetDaysOfYear(this in DateTime value)
	{
		//取得传入参数的年份部分，用来判断是否是闰年
		var n = value.Year;
		return IsRuYear(n) ? 366 : 365;
	}

	/// <summary>
	/// 获取指定年份和月份的当月总天数。
	/// </summary>
	/// <param name="_">当前 DateTime 实例（未使用）。</param>
	/// <param name="iYear">目标年份。</param>
	/// <param name="month">目标月份。</param>
	/// <returns>该月份的总天数。</returns>
	public static int GetDaysOfMonth(this DateTime _, int iYear, int month)
	{
		return month switch
		{
			1 => 31,
			2 => (IsRuYear(iYear) ? 29 : 28),
			3 => 31,
			4 => 30,
			5 => 31,
			6 => 30,
			7 => 31,
			8 => 31,
			9 => 30,
			10 => 31,
			11 => 30,
			12 => 31,
			_ => 0
		};
	}

	/// <summary>
	/// 获取指定日期对应月份的总天数。
	/// </summary>
	/// <param name="value">输入的日期。</param>
	/// <returns>该月份的总天数。</returns>
	public static int GetDaysOfMonth(this in DateTime value)
	{
		// Uses the year and month information to get the number of days in the current month.
		return value.Month switch
		{
			1 => 31,
			2 => (IsRuYear(value.Year) ? 29 : 28),
			3 => 31,
			4 => 30,
			5 => 31,
			6 => 30,
			7 => 31,
			8 => 31,
			9 => 30,
			10 => 31,
			11 => 30,
			12 => 31,
			_ => 0
		};
	}

	/// <summary>
	/// 获取指定日期对应的中文星期名称。
	/// </summary>
	/// <param name="value">输入的日期。</param>
	/// <returns>中文星期名称（如“星期一”）。</returns>
	public static string GetWeekNameOfDay(this in DateTime value)
	{
		return value.DayOfWeek.ToString() switch
		{
			"Mondy" => "星期一",
			"Tuesday" => "星期二",
			"Wednesday" => "星期三",
			"Thursday" => "星期四",
			"Friday" => "星期五",
			"Saturday" => "星期六",
			"Sunday" => "星期日",
			_ => ""
		};
	}

	/// <summary>
	/// 获取指定日期对应的数字形式的星期编号（1~7）。
	/// </summary>
	/// <param name="value">输入的日期。</param>
	/// <returns>数字形式的星期编号（例如："1" 表示周一）。</returns>
	public static string GetWeekNumberOfDay(this in DateTime value)
	{
		return value.DayOfWeek.ToString() switch
		{
			"Mondy" => "1",
			"Tuesday" => "2",
			"Wednesday" => "3",
			"Thursday" => "4",
			"Friday" => "5",
			"Saturday" => "6",
			"Sunday" => "7",
			_ => ""
		};
	}

	/// <summary>
	/// 判断一个年份是否为闰年。
	/// </summary>
	/// <param name="value">需要判断的年份。</param>
	/// <returns>如果是闰年则返回 true，否则返回 false。</returns>
	private static bool IsRuYear(int value)
	{
		// The parameter is the year.
		// Example: 2003
		var n = value;
		return n % 400 == 0 || n % 4 == 0 && n % 100 != 0;
	}

	/// <summary>
	/// 验证输入的字符串是否是一个有效的日期，并且大于等于 1800 年 1 月 1 日。
	/// </summary>
	/// <param name="value">待验证的日期字符串。</param>
	/// <returns>如果有效并满足条件返回 true，否则返回 false。</returns>
	public static bool IsDateTime(this string value)
	{
		_ = DateTime.TryParse(value, out var result);
		return result.CompareTo(DateTime.Parse("1800-1-1")) > 0;
	}

	/// <summary>
	/// 检查某个时间是否落在指定范围内。
	/// </summary>
	/// <param name="dateTime">被检查的时间。</param>
	/// <param name="start">范围起点。</param>
	/// <param name="end">范围终点。</param>
	/// <param name="mode">区间模式（开、闭、左开右闭等）。</param>
	/// <returns>若时间在范围内返回 true，否则返回 false。</returns>
	public static bool In(this in DateTime dateTime, DateTime start, DateTime end, RangeMode mode = RangeMode.Close)
	{
		return mode switch
		{
			RangeMode.Open => start < dateTime && end > dateTime,
			RangeMode.Close => start <= dateTime && end >= dateTime,
			RangeMode.OpenClose => start < dateTime && end >= dateTime,
			RangeMode.CloseOpen => start <= dateTime && end > dateTime,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	/// <summary>
	/// 获取指定月份的第一天与最后一天的日期字符串。
	/// </summary>
	/// <param name="_">当前 DateTime 实例（未使用）。</param>
	/// <param name="month">目标月份。</param>
	/// <param name="firstDay">输出参数：该月第一天的字符串表示。</param>
	/// <param name="lastDay">输出参数：该月最后一天的字符串表示。</param>
	public static void GetDateFormat(this DateTime _, int month, out string firstDay, out string lastDay)
	{
		var year = DateTime.Now.Year + month / 12;
		if (month != 12)
		{
			month %= 12;
		}

		switch (month)
		{
			case 1:
				firstDay = DateTime.Now.ToString($"{year}-0{month}-01");
				lastDay = DateTime.Now.ToString($"{year}-0{month}-31");
				break;

			case 2:
				firstDay = DateTime.Now.ToString(year + "-0" + month + "-01");
				lastDay = DateTime.IsLeapYear(DateTime.Now.Year) ? DateTime.Now.ToString(year + "-0" + month + "-29") : DateTime.Now.ToString(year + "-0" + month + "-28");
				break;

			case 3:
				firstDay = DateTime.Now.ToString(year + "-0" + month + "-01");
				lastDay = DateTime.Now.ToString("yyyy-0" + month + "-31");
				break;

			case 4:
				firstDay = DateTime.Now.ToString(year + "-0" + month + "-01");
				lastDay = DateTime.Now.ToString(year + "-0" + month + "-30");
				break;

			case 5:
				firstDay = DateTime.Now.ToString(year + "-0" + month + "-01");
				lastDay = DateTime.Now.ToString(year + "-0" + month + "-31");
				break;

			case 6:
				firstDay = DateTime.Now.ToString(year + "-0" + month + "-01");
				lastDay = DateTime.Now.ToString(year + "-0" + month + "-30");
				break;

			case 7:
				firstDay = DateTime.Now.ToString(year + "-0" + month + "-01");
				lastDay = DateTime.Now.ToString(year + "-0" + month + "-31");
				break;

			case 8:
				firstDay = DateTime.Now.ToString(year + "-0" + month + "-01");
				lastDay = DateTime.Now.ToString(year + "-0" + month + "-31");
				break;

			case 9:
				firstDay = DateTime.Now.ToString(year + "-0" + month + "-01");
				lastDay = DateTime.Now.ToString(year + "-0" + month + "-30");
				break;

			case 10:
				firstDay = DateTime.Now.ToString(year + "-" + month + "-01");
				lastDay = DateTime.Now.ToString(year + "-" + month + "-31");
				break;

			case 11:
				firstDay = DateTime.Now.ToString(year + "-" + month + "-01");
				lastDay = DateTime.Now.ToString(year + "-" + month + "-30");
				break;

			default:
				firstDay = DateTime.Now.ToString(year + "-" + month + "-01");
				lastDay = DateTime.Now.ToString(year + "-" + month + "-31");
				break;
		}
	}

	/// <summary>
	/// 获取指定年份和月份的最后一天是几号。
	/// </summary>
	/// <param name="_">当前 DateTime 实例（未使用）。</param>
	/// <param name="year">目标年份。</param>
	/// <param name="month">目标月份。</param>
	/// <returns>该月的最后一天的日期。</returns>
	public static int GetMonthLastDate(this DateTime _, int year, int month)
	{
		var lastDay = new DateTime(year, month, new GregorianCalendar().GetDaysInMonth(year, month));
		var day = lastDay.Day;
		return day;
	}

	/// <summary>
	/// 获取两个时间之间的时差（HH:mm:ss 格式）。
	/// </summary>
	/// <param name="dtStart">起始时间。</param>
	/// <param name="dtEnd">结束时间。</param>
	/// <returns>格式化的时差字符串（HH:mm:ss）。</returns>
	public static string GetTimeDelay(this in DateTime dtStart, DateTime dtEnd)
	{
		var lTicks = (dtEnd.Ticks - dtStart.Ticks) / 10000000;
		var sTemp = (lTicks / 3600).ToString().PadLeft(2, '0') + ":";
		sTemp += (lTicks % 3600 / 60).ToString().PadLeft(2, '0') + ":";
		sTemp += (lTicks % 3600 % 60).ToString().PadLeft(2, '0');
		return sTemp;
	}

	/// <summary>
	/// 获取日期的整型字符串表示（YYYYMMDD）。
	/// </summary>
	/// <param name="value">输入的日期。</param>
	/// <returns>格式为 YYYYMMDD 的字符串。</returns>
	public static string GetDateString(this in DateTime value)
	{
		return value.Year + value.Month.ToString().PadLeft(2, '0') + value.Day.ToString().PadLeft(2, '0');
	}

	/// <summary>
	/// 获取两个日期之间的时间差异描述。
	/// </summary>
	/// <param name="dateTime1">第一个日期。</param>
	/// <param name="dateTime2">第二个日期。</param>
	/// <returns>时间差异描述（如“X小时前”、“Y分钟前”或“X月Y日”）。</returns>
	public static string DateDiff(this in DateTime dateTime1, in DateTime dateTime2)
	{
		string dateDiff;
		var ts = dateTime2 - dateTime1;
		if (ts.Days >= 1)
		{
			dateDiff = dateTime1.Month + "月" + dateTime1.Day + "日";
		}
		else
		{
			dateDiff = ts.Hours > 1 ? ts.Hours + "小时前" : ts.Minutes + "分钟前";
		}

		return dateDiff;
	}

	/// <summary>
	/// 计算两个时间之间的详细时间间隔（年/月/天/小时/分/秒）。
	/// </summary>
	/// <param name="beginTime">起始时间。</param>
	/// <param name="endTime">结束时间。</param>
	/// <returns>详细的时差描述字符串。</returns>
	public static string GetDiffTime(this in DateTime beginTime, in DateTime endTime)
	{
		var strResout = string.Empty;
		// Gets the time interval in seconds between two times.
		var span = endTime.Subtract(beginTime);
		var sec = Convert.ToInt32(span.TotalSeconds);
		var minutes = 1 * 60;
		var hours = minutes * 60;
		var day = hours * 24;
		var month = day * 30;
		var year = month * 12;

		// Reminder time: returns 1 if the time has arrived, otherwise returns 0.
		if (sec > year)
		{
			strResout += sec / year + "年";
			sec %= year; // Remaining
		}

		if (sec > month)
		{
			strResout += sec / month + "月";
			sec %= month;
		}

		if (sec > day)
		{
			strResout += sec / day + "天";
			sec %= day;
		}

		if (sec > hours)
		{
			strResout += sec / hours + "小时";
			sec %= hours;
		}

		if (sec > minutes)
		{
			strResout += sec / minutes + "分";
			sec %= minutes;
		}

		strResout += sec + "秒";
		return strResout;
	}

	/// <summary>
	/// 转换为标准日期字符串格式（"yyyy-MM-dd"）。
	/// </summary>
	/// <param name="value">输入的日期。</param>
	/// <returns>格式化后的日期字符串。</returns>
	public static string ToStandardDateString(this DateTime value)
	{
		return value.ToString("yyyy-MM-dd");
	}

	public static string ToStandardTimeHHMMString(this DateTime value) => DateTime.Now.ToString("yyyy-MM-dd HH:mm");

	/// <summary>
	/// 转换为标准时间字符串格式（"yyyy-MM-dd HH:mm:ss"）。
	/// </summary>
	/// <param name="value">输入的日期时间。</param>
	/// <returns>格式化后的时间字符串。</returns>
	public static string ToStandardTimeString(this DateTime value)
	{
		return value.ToString("yyyy-MM-dd HH:mm:ss");
	}

	/// <summary>
	/// 转换为完整精度的标准时间字符串格式（"yyyy-MM-dd HH:mm:ss:fffffff"）。
	/// </summary>
	/// <param name="value">输入的日期时间。</param>
	/// <returns>高精度的时间字符串。</returns>
	public static string ToStandardFullTimeString(this in DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss:fffffff");

	/// <summary>
	/// 将当前时间转换到特定的目标时区。
	/// </summary>
	/// <param name="value">源时间。</param>
	/// <param name="destinationTimeZone">目标时区信息。</param>
	/// <returns>转换后的时间。</returns>
	public static DateTime ConvertTime(this DateTime value, TimeZoneInfo destinationTimeZone)
	{
		return TimeZoneInfo.ConvertTime(value, destinationTimeZone);
	}

	/// <summary>
	/// 在两个不同的时区间进行时间转换。
	/// </summary>
	/// <param name="value">源时间。</param>
	/// <param name="sourceTimeZone">源时区信息。</param>
	/// <param name="destinationTimeZone">目标时区信息。</param>
	/// <returns>转换后的时间。</returns>
	public static DateTime ConvertTime(this DateTime value, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
	{
		return TimeZoneInfo.ConvertTime(value, sourceTimeZone, destinationTimeZone);
	}

	/// <summary>
	/// 判断指定日期是否为工作日（即非周末）。
	/// </summary>
	/// <param name="value">输入的日期。</param>
	/// <returns>如果是工作日返回 true，否则返回 false。</returns>
	public static bool IsWeekDay(this DateTime value)
	{
		return !(value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek == DayOfWeek.Sunday);
	}

	/// <summary>
	/// 判断指定日期是否为周末。
	/// </summary>
	/// <param name="value">输入的日期。</param>
	/// <returns>如果是周末返回 true，否则返回 false。</returns>
	public static bool IsWeekendDay(this DateTime value)
	{
		return value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek == DayOfWeek.Sunday;
	}

	/// <summary>
	/// 计算指定出生日期至今的年龄。
	/// </summary>
	/// <param name="value">出生日期。</param>
	/// <returns>当前年龄。</returns>
	public static int GetAge(this DateTime value)
	{
		if (DateTime.Today.Month < value.Month ||
			DateTime.Today.Month == value.Month &&
			DateTime.Today.Day < value.Day)
		{
			return DateTime.Today.Year - value.Year - 1;
		}
		return DateTime.Today.Year - value.Year;
	}
}

/// <summary>
/// 定义时间范围比较的不同模式。
/// </summary>
public enum RangeMode
{
	/// <summary>
	/// 开区间（不包括边界）
	/// </summary>
	Open,

	/// <summary>
	/// 闭区间（包括边界）
	/// </summary>
	Close,

	/// <summary>
	/// 左开右闭区间（左边界排除，右边界包含）
	/// </summary>
	OpenClose,

	/// <summary>
	/// 左闭右开区间（左边界包含，右边界排除）
	/// </summary>
	CloseOpen
}
