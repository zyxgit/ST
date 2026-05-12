namespace ST.MS.Identity.Application.Options;

public class IdentitySessionOptions
{
	public const string SectionName = "Identity:Session";

	public int MaxActiveRefreshSessionsPerUser { get; set; } = 5;
}
