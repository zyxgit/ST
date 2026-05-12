using ST.Infra.Repository.Interface;
using ST.Shared.Security;

namespace ST.Shared.WebApi.Authentication;

public sealed class HttpCurrentUserIdAccessor : ICurrentUserIdAccessor
{
	private readonly IUserContext _userContext;

	public HttpCurrentUserIdAccessor(IUserContext userContext)
	{
		_userContext = userContext;
	}

	public Guid? UserId => _userContext.UserId;
}
