namespace ST.Shared.Security;

public interface IRefreshTokenLifetimeProvider
{
	TimeSpan GetLifetime();
}
