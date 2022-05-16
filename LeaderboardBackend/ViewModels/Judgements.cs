namespace LeaderboardBackend.ViewModels;

/// <summary>A decision by a mod on a run submission.</summary>
/// <remarks>
/// Refer to docs in Models/Entities/Judgement.cs.
/// </remarks>
public readonly record struct JudgementViewModel
{
	/// <summary>Generated on creation.</summary>
	public readonly long Id;

	/// <summary>
	/// Defines this judgement, which in turn defines the status of its related run. <br />
	/// If:
	///   <ul>
	///     <li>true, run is approved;</li>
	///     <li>false, run is rejected;</li>
	///     <li>null, run is commented on.</li>
	///   </ul>
	/// For the latter two, Note MUST be non-empty.
	/// </summary>
	/// <example>true</example>
	public readonly bool? Approved;

	/// <summary>When the judgement was made.</summary>
	public readonly string CreatedAt;

	/// <summary>
	/// Comments on the judgement.
	/// MUST be non-empty for rejections or comments (Approved ∈ {false, null}).
	/// </summary>
	public readonly string? Note;

	/// <summary>ID of the mod that made this judgement.</summary>
	public readonly Guid ModId;

	/// <summary>ID of the related run.</summary>
	public readonly Guid RunId;

	public JudgementViewModel(
		long id,
		bool? approved,
		DateTime createdAt,
		string? note,
		Guid modId,
		Guid runId
	)
	{
		Id = id;
		Approved = approved;
		CreatedAt = createdAt.ToLongDateString();
		Note = note;
		ModId = modId;
		RunId = runId;
	}
}
