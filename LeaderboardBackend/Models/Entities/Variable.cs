using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LeaderboardBackend.Models.Entities;

// TODO: Fix this definition.
/// <summary>
///     Represents a Variable.
/// </summary>
/// <remarks>
///     Values tied to this `Variable` have its own `Variable Value` model.
/// </remarks>
public class Variable
{
	/// <summary>
	///     The unique identifier of the `Variable`.<br/>
	///     Generated on creation.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     The name of the `Variable`.
	/// </summary>
	/// <example>Platform</example>
	[NotNull]
	[Required]
	public string? Name { get; set; }

	/// <summary>
	///     The slug of the `Variable`, a.k.a. its representation in the URL.
	/// </summary>
	/// <example>platform</example>
	[NotNull]
	[Required]
	public string? Slug { get; set; }

	/// <summary>
	///     A collection `Category`s on the `Variable`.
	/// </summary>
	public List<Category>? Categories { get; set; }

	// TODO: Figure out how to compare Categories too
	public override bool Equals(object? obj)
	{
		return obj is Variable variable
			&& Id == variable.Id
			&& Name == variable.Name
			&& Slug == variable.Slug;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Name, Slug);
	}
}
