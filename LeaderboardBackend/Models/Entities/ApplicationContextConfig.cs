using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Entities;

public class ApplicationContextConfig : IValidatableObject
{
    public const string KEY = "ApplicationContext";

    public bool MigrateDb { get; set; } = false;
    public bool UseInMemoryDb { get; set; } = false;
    public PostgresConfig? Pg { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!UseInMemoryDb && Pg == null)
        {
            yield return new ValidationResult(
                "Missing database configuration.",
                new[] { nameof(UseInMemoryDb), nameof(Pg) }
            );
        }
    }
}

public class PostgresConfig
{
    [Required]
    public required string Host { get; set; }

    [Required]
    public required string User { get; set; }

    [Required]
    public required string Password { get; set; }

    [Required]
    public required string Db { get; set; }
    public ushort? Port { get; set; }
}
