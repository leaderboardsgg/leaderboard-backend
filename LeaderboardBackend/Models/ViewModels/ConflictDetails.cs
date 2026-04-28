using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Models.ViewModels;

public class ConflictDetails<T> : ProblemDetails
{
    [JsonPropertyName("conflicting")]
    public required T Conflicting { get; set; }
}
