namespace ST.MS.FileUpload.Domain.Entities;

/// <summary>
/// 文件访问级别
/// </summary>
public enum FileAccessLevel
{
    /// <summary>公开（任意认证用户可下载）</summary>
    Public = 0,

    /// <summary>私有（需特定权限或资源归属）</summary>
    Private = 1
}
