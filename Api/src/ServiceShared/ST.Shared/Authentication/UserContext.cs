namespace ST.Shared.Authentication;

public class UserContext : IUserContext
{
	Guid UserId { get; set; }

	string Email { get; set; }

	string Phone { get; set; }

	string Name {  get; set; }


}
