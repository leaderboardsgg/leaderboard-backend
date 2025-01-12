using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Models.ViewModels;

/// <summary>
///     A fake ProblemDetails subclass used for deserialization and documentation. Do not instantiate!
/// </summary>
public class ConflictDetails<T> : ProblemDetails
{
    [JsonPropertyName("conflicting")]
    public T? Conflicting { get; set; }
}
