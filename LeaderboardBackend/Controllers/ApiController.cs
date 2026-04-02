using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public abstract class ApiController : ControllerBase { }
