using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents an option for a `Variable` for a `Run`.
/// </summary>
public class VariableValue
{
	/// <summary>
	///     The unique identifier of the `VariableValue`.<br/>
	///     Generated on creation.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     The name of the `VariableValue`. This is what is shown to the user
	///     as an option to set a `Value` to for a `Run`.
	/// </summary>
	/// <example>PS2</example>
	[NotNull]
	[Required]
	public string? Name { get; set; }

	/// <summary>
	///     A collection of `Run`s on the `VariableValue`.
	/// </summary>
	public List<Run>? Runs { get; set; }
}
