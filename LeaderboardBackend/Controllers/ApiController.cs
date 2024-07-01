using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("api/[controller]")]
[SwaggerResponse(400, Type = typeof(ProblemDetails))]
[SwaggerResponse(500)]
public abstract class ApiController : ControllerBase { }
