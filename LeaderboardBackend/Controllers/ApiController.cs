using LeaderboardBackend.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public abstract class ApiController : ControllerBase;
