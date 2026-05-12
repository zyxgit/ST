using Microsoft.AspNetCore.Authorization;

namespace ST.Shared.WebApi.Controller;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AbstractControllerBase : ControllerBase
{
}
