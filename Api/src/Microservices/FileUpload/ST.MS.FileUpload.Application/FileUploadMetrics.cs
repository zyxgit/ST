using System.Diagnostics.Metrics;

namespace ST.MS.FileUpload.Application;

/// <summary>
/// FileUpload 服务自定义 OpenTelemetry 指标。
/// Meter 名称：ST.FileUpload
/// </summary>
public static class FileUploadMetrics
{
	public static readonly Meter Meter = new("ST.FileUpload", "1.0.0");

	/// <summary>上传成功数</summary>
	public static readonly Counter<long> UploadCount =
		Meter.CreateCounter<long>("st.fileupload.count", description: "上传成功数");

	/// <summary>上传失败数</summary>
	public static readonly Counter<long> UploadFailed =
		Meter.CreateCounter<long>("st.fileupload.failed", description: "上传失败数");

	/// <summary>文件大小分布 (bytes)</summary>
	public static readonly Histogram<double> FileSizeBytes =
		Meter.CreateHistogram<double>("st.fileupload.size_bytes", description: "文件大小分布(bytes)");
}
