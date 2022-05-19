using LeaderboardBackend.Models.Requests;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Annotations;

/// <summary>Asserts that Note is non-empty for non-approval judgements (Approved is false or null).</summary>
public class NoteAttribute : ValidationAttribute {
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		string note = (string)value!;

		CreateJudgementRequest request = (CreateJudgementRequest)validationContext.ObjectInstance;

		if (request.Note.Length == 0 && (request.Approved is null || request.Approved is false))
		{
			return new ValidationResult("Notes must be provided with this judgement.");
		}

		return ValidationResult.Success;
	}
}
