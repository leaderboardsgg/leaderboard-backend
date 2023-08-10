using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
public class User
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
    [Required]
    public string Username { get; set; } = null!;

    /// <summary>
    ///     The `User`'s email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required]
    public string Email { get; set; } = null!;

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
    [JsonIgnore]
    [Required]
    public string Password { get; set; } = null!;

    /// <summary>
    /// User role (site-wide)
    /// </summary>
    [Required]
    public UserRole Role { get; set; } = UserRole.Registered;

    public bool IsAdmin => Role == UserRole.Administrator;

    public override bool Equals(object? obj)
    {
        return obj is User user && Id.Equals(user.Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Username, Email);
    }
}

public class UserEntityTypeConfig : IEntityTypeConfiguration<User>
{
    public const string USERNAME_UNIQUE_INDEX = "ix_users_username";
    public const string EMAIL_UNIQUE_INDEX = "ix_users_email";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.Username)
            .UseCollation(ApplicationContext.CASE_INSENSITIVE_COLLATION);

        builder.Property(x => x.Email)
            .UseCollation(ApplicationContext.CASE_INSENSITIVE_COLLATION);

        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName(USERNAME_UNIQUE_INDEX);

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName(EMAIL_UNIQUE_INDEX);
    }
}
