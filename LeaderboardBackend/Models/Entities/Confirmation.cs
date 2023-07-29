using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents a user account confirmation.
/// </summary>
public class Confirmation
{
    /// <summary>
    ///     The unique identifier of the `Confirmation`.<br/>
    ///     Generated on creation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     The ID of the `User` the `Confirmation` is a part of.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    ///     Relationship model for `UserId`.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    ///     The time this `Confirmation` is created.
    /// </summary>
    public Instant CreatedAt { get; set; } = SystemClock.Instance.GetCurrentInstant();

    /// <summary>
    ///     The time this `Confirmation` is used.
    /// </summary>
    public Instant? UsedAt { get; set; }

    /// <summary>
    ///     The time this `Confirmation` expires, after which the associated `User` must
    ///     request for another confirmation email.
    /// </summary>
    public Instant ExpiresAt { get; set; } = SystemClock.Instance.GetCurrentInstant() + Duration.FromHours(1);
}
