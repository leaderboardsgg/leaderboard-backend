using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents a user account confirmation.
/// </summary>
public class AccountConfirmation : IHasCreationTimestamp
{
    /// <summary>
    ///     The unique identifier of the `AccountConfirmation`.<br/>
    ///     Generated on creation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     The ID of the `User` the `AccountConfirmation` is a part of.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     Relationship model for `UserId`.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    ///     The time this `AccountConfirmation` is created.
    /// </summary>
    public Instant CreatedAt { get; set; }

    /// <summary>
    ///     The time this `AccountConfirmation` is used.
    /// </summary>
    public Instant? UsedAt { get; set; }

    /// <summary>
    ///     The time this `AccountConfirmation` expires, after which the associated `User` must
    ///     request for another confirmation email.
    /// </summary>
    public Instant ExpiresAt { get; set; }
}
