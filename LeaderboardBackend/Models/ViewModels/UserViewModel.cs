using System.Text.Json.Serialization;
using LeaderboardBackend.Models.Entities;
using NodaTime;

namespace LeaderboardBackend.Models.ViewModels;

public record UserViewModel
{
    /// <summary>
    ///     The unique identifier of the `User`.<br/>
    ///     Generated on creation.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    ///     The username of the `User`. It:
    ///     <ul>
    ///       <li>must be [2, 25] in length;</li>
    ///       <li>must be made up of alphanumeric characters around zero or one of:</li>
    ///       <ul>
    ///         <li>hyphen;</li>
    ///         <li>underscore; or</li>
    ///         <li>apostrophe</li>
    ///       </ul>
    ///     </ul>
    ///     Usernames are saved case-sensitively, but matcehd against case-insensitively.
    ///     A `User` may not register with the name 'Cool' when another `User` with the name 'cool'
    ///     exists.
    /// </summary>
    /// <example>J'on-Doe</example>
    public required string Username { get; set; }

    public required UserRole Role { get; set; }

    public required Instant CreatedAt { get; set; }

    public static UserViewModel MapFrom(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Role = user.Role,
        CreatedAt = user.CreatedAt
    };
}
