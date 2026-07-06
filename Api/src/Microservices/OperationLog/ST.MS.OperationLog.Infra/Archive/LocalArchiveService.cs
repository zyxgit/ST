using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.MS.OperationLog.Infra.DbContext;

namespace ST.MS.OperationLog.Infra.Archive;

/// <summary>
/// 本地文件系统归档服务实现。
/// </summary>
public sealed class LocalArchiveService : IArchiveService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		WriteIndented = true
	};

	private readonly OperationLogDbContext _dbContext;
	private readonly OperationLogArchiveOptions _options;
	private readonly ILogger<LocalArchiveService> _logger;

	public LocalArchiveService(
		OperationLogDbContext dbContext,
		IOptions<OperationLogArchiveOptions> options,
		ILogger<LocalArchiveService> logger)
	{
		_dbContext = dbContext;
		_options = options.Value;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task<ArchiveResult> ArchiveAsync(CancellationToken cancellation = default)
	{
		var cutoffDate = DateTime.UtcNow.AddDays(-_options.ArchiveAfterDays);

		_logger.LogInformation("Starting archive operation. Cutoff date: {CutoffDate}", cutoffDate);

		try
		{
			// 查询需要归档的日志
			var logsToArchive = await _dbContext.OperationLogs
				.Where(l => l.CreatedAtUtc < cutoffDate)
				.OrderBy(l => l.CreatedAtUtc)
				.Take(_options.BatchSize)
				.ToListAsync(cancellation);

			if (logsToArchive.Count == 0)
			{
				_logger.LogInformation("No logs to archive.");
				return new ArchiveResult
				{
					Success = true,
					ArchivedCount = 0
				};
			}

			// 转换为归档格式
			var archivedEntries = logsToArchive.Select(l => new ArchivedLogEntry
			{
				CreatedAtUtc = l.CreatedAtUtc,
				ServiceName = l.ServiceName,
				TraceId = l.TraceId,
				SpanId = l.SpanId,
				UserId = l.UserId,
				UserName = l.UserName,
				OperationName = l.OperationName,
				Path = l.Path,
				Method = l.Method,
				Ip = l.Ip,
				StatusCode = l.StatusCode,
				Success = l.Success,
				DurationMs = l.DurationMs,
				RequestJson = l.RequestJson,
				ResponseJson = l.ResponseJson,
				ExceptionType = l.ExceptionType,
				ExceptionMessage = l.ExceptionMessage,
				ExceptionStackTrace = l.ExceptionStackTrace
			}).ToList();

			// 生成归档文件路径
			var archiveDate = logsToArchive.First().CreatedAtUtc;
			var archivePath = Path.Combine(
				_options.LocalArchivePath,
				archiveDate.ToString("yyyy"),
				archiveDate.ToString("MM"),
				archiveDate.ToString("dd"));

			Directory.CreateDirectory(archivePath);

			var fileName = $"{_options.FilePrefix}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.json";
			var filePath = Path.Combine(archivePath, fileName);

			// 序列化并写入文件
			var json = JsonSerializer.Serialize(archivedEntries, JsonOptions);
			await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellation);

			_logger.LogInformation("Archived {Count} logs to {FilePath}", logsToArchive.Count, filePath);

			// 删除已归档的日志
			if (_options.DeleteAfterArchive)
			{
				_dbContext.OperationLogs.RemoveRange(logsToArchive);
				await _dbContext.SaveChangesAsync(cancellation);
				_logger.LogInformation("Deleted {Count} archived logs from database.", logsToArchive.Count);
			}

			return new ArchiveResult
			{
				Success = true,
				ArchivedCount = logsToArchive.Count,
				ArchiveFilePath = filePath,
				StartTime = logsToArchive.First().CreatedAtUtc,
				EndTime = logsToArchive.Last().CreatedAtUtc
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Archive operation failed.");
			return new ArchiveResult
			{
				Success = false,
				ErrorMessage = ex.Message
			};
		}
	}

	/// <inheritdoc />
	public async Task<List<ArchivedLogEntry>> QueryArchiveAsync(DateTime startTime, DateTime endTime, CancellationToken cancellation = default)
	{
		var results = new List<ArchivedLogEntry>();

		// 扫描归档目录
		var archiveRoot = _options.LocalArchivePath;
		if (!Directory.Exists(archiveRoot))
		{
			return results;
		}

		// 遍历日期目录
		var yearDirs = Directory.GetDirectories(archiveRoot);
		foreach (var yearDir in yearDirs)
		{
			var monthDirs = Directory.GetDirectories(yearDir);
			foreach (var monthDir in monthDirs)
			{
				var dayDirs = Directory.GetDirectories(monthDir);
				foreach (var dayDir in dayDirs)
				{
					// 检查日期是否在查询范围内
					if (DateTime.TryParse($"{Path.GetFileName(yearDir)}-{Path.GetFileName(monthDir)}-{Path.GetFileName(dayDir)}", out var dirDate))
					{
						if (dirDate < startTime.Date || dirDate > endTime.Date.AddDays(1))
						{
							continue;
						}
					}

					// 读取该日期下的所有归档文件
					var files = Directory.GetFiles(dayDir, $"{_options.FilePrefix}_*.json");
					foreach (var file in files)
					{
						try
						{
							var json = await File.ReadAllTextAsync(file, Encoding.UTF8, cancellation);
							var entries = JsonSerializer.Deserialize<List<ArchivedLogEntry>>(json, JsonOptions);

							if (entries is not null)
							{
								// 按时间范围筛选
								var filtered = entries.Where(e =>
									e.CreatedAtUtc >= startTime &&
									e.CreatedAtUtc <= endTime).ToList();

								results.AddRange(filtered);
							}
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "Failed to read archive file: {FilePath}", file);
						}
					}
				}
			}
		}

		return results.OrderBy(e => e.CreatedAtUtc).ToList();
	}
}
