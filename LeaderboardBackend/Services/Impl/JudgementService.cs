using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public class JudgementService : IJudgementService
{
	private readonly ApplicationContext _applicationContext;

	public JudgementService(ApplicationContext applicationContext)
	{
		_applicationContext = applicationContext;
	}

	public async Task<Judgement?> GetJudgement(long id)
	{
		return await _applicationContext.Judgements
			.FindAsync(id);
	}

	public async Task CreateJudgement(Judgement judgement)
	{
		_applicationContext.Judgements.Add(judgement);
		await _applicationContext.SaveChangesAsync();
	}
}
