using LeaderboardBackend.Models.Annotations;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>Request object sent when creating a Judgement.</summary>
public record CreateJudgementRequest
{
	[Required]
	public Guid RunId;

	/// <summary>See related property in model.</summary>
	[Required]
	[Note]
	public string Note = "";

	/// <summary>See related property in model.</summary>
	public bool? Approved;

}
