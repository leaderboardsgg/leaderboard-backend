using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LeaderboardBackend.Models.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

public enum UserRole
{
    Registered = 1,
    Confirmed,
    Administrator,
    Banned,
}

/// <summary>
///     Represents a user account registered on the website.
/// </summary>
public class User : IHasCreationTimestamp
{
    /// <summary>
    ///     The unique identifier of the `User`.<br/>
    ///     Generated on creation.
    /// </summary>
    public Guid Id { get; set; }

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
    ///     Usernames are saved case-sensitively, but matched against case-insensitively.
    ///     A `User` may not register with the name 'Cool' when another `User` with the name 'cool'
    ///     exists.
    /// </summary>
    /// <example>J'on-Doe</example>
    [Column(TypeName = "citext")]
    [RegularExpression(UsernameRule.REGEX)]
    [StringLength(25, MinimumLength = 2)]
    public required string Username { get; set; }

    /// <summary>
    ///     The `User`'s email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Column(TypeName = "citext")]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    ///     The `User`'s password. It:
    ///     <ul>
    ///       <li>supports Unicode;</li>
    ///       <li>must be [8, 80] in length;</li>
    ///       <li>must have at least:</li>
    ///         <ul>
    ///           <li>one uppercase letter;</li>
    ///           <li>one lowercase letter; and</li>
    ///           <li>one number.</li>
    ///         </ul>
    ///     </ul>
    /// </summary>
    /// <example>P4ssword</example>
    [Required]
    public required string Password { get; set; }

    /// <summary>
    /// User role (site-wide)
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Registered;

    public Instant CreatedAt { get; set; }

    public bool IsAdmin => Role == UserRole.Administrator;
}

public class UserEntityTypeConfig : IEntityTypeConfiguration<User>
{
    public const string USERNAME_UNIQUE_INDEX = "ix_users_username";
    public const string EMAIL_UNIQUE_INDEX = "ix_users_email";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName(USERNAME_UNIQUE_INDEX);

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName(EMAIL_UNIQUE_INDEX);
    }
}
