using LeaderboardBackend.Models.Requests;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Annotations;

public class NoteAttribute : ValidationAttribute {
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		// Note defaults to "" in the model itself
		string note = (string)value!;

		CreateJudgementRequest request = (CreateJudgementRequest)validationContext.ObjectInstance;

		if (request.Note.Length == 0 && (request.Approved is null || request.Approved is false))
		{
			return new ValidationResult("Notes must be provided with this judgement.");
		}

		return ValidationResult.Success;
	}
}
