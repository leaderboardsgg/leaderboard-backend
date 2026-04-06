using LeaderboardBackend.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    [NonAction]
    public ConflictDetails<T> CreateConflictDetails<T>(T conflicting)
    {
        ConflictDetails<T> conflictDetails = (ConflictDetails<T>)ProblemDetailsFactory.CreateProblemDetails(
            HttpContext,
            409
        );

        conflictDetails.Conflicting = conflicting;
        return conflictDetails;
    }
}
