using System.ComponentModel.DataAnnotations;
using NodaTime;

/// <summary>
///     Base class that provides timestamp fields to all Entities.
/// </summary>
public class Timestamps
{
	/// <summary>
	///     The time this entity was created.
	/// </summary>
	[Required]
	public Instant CreatedAt { get; set; }

	/// <summary>
	///     The time this entity was updated.
	/// </summary>
	[Required]
	public Instant UpdatedAt { get; set; }

	/// <summary>
	///     The time this entity was deleted.
	/// </summary>
	public Instant? DeletedAt { get; set; }
}
