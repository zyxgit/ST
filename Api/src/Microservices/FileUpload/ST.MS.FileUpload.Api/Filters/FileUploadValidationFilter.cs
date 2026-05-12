using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Domain;

namespace ST.MS.FileUpload.Api.Filters;

/// <summary>
/// 文件上传验证过滤器
/// 在 action 执行前校验文件类型、扩展名、大小
/// </summary>
public sealed class FileUploadValidationFilter : IActionFilter
{
    private readonly FileStorageOptions _options;

    public FileUploadValidationFilter(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var file = context.ActionArguments.Values.OfType<IFormFile>().FirstOrDefault();
        if (file == null || file.Length == 0) return;

        // 检查文件大小
        if (file.Length > _options.MaxFileSize)
        {
            var maxMb = _options.MaxFileSize / (1024.0 * 1024.0);
            throw new BusinessException($"文件大小超出限制（最大 {maxMb:F0}MB）");
        }

        // 检查 MIME 类型白名单
        if (_options.AllowedContentTypes is { Length: > 0 } &&
            !_options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException($"不支持的文件类型：{file.ContentType}");
        }

        // 检查扩展名白名单
        if (_options.AllowedExtensions is { Length: > 0 })
        {
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) ||
                !_options.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                throw new BusinessException($"不支持的文件扩展名：{ext}");
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
