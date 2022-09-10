using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Requests;

namespace LeaderboardBackend.Models.Attributes;

public class PlayersMaxAttribute : ValidationAttribute
{
	protected override ValidationResult? IsValid(object? _, ValidationContext context)
	{
		CreateCategoryRequest request = (CreateCategoryRequest)context.ObjectInstance;

		if (request.PlayersMax is null)
		{
			return ValidationResult.Success;
		}

		if (request.PlayersMax < request.PlayersMin)
		{
			return new(
				$"playersMax ({request.PlayersMax}) must be at least equal to " +
				$"playersMin ({request.PlayersMin}).");
		}

		return ValidationResult.Success;
	}
}
