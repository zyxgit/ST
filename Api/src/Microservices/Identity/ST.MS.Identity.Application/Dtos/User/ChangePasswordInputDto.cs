namespace ST.MS.Identity.Application.Dtos.User;

public sealed class ChangePasswordInputDto
{
    public string OldPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
