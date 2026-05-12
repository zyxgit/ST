namespace ST.MS.Identity.Application.Dtos.User;

public sealed class SetUserAvatarInputDto
{
    /// <summary>头像文件ID（来自 FileUpload 服务）</summary>
    public Guid? AvatarFileId { get; set; }
}
