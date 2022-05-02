using LeaderboardBackend.Models.Requests;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Annotations;

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
			return new ValidationResult(
				$"playersMax ({request.PlayersMax}) must be at least equal to playersMin ({request.PlayersMin})."
			);
		}

		return ValidationResult.Success;
	}
}
