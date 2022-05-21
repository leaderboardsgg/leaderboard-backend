using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public class JudgementService : IJudgementService
{
	private readonly ApplicationContext ApplicationContext;

	public JudgementService(ApplicationContext applicationContext)
	{
		ApplicationContext = applicationContext;
	}

	public async Task<Judgement?> GetJudgement(long id)
	{
		return await ApplicationContext.Judgements.FindAsync(id);
	}

	public async Task CreateJudgement(Judgement judgement)
	{
		ApplicationContext.Judgements.Add(judgement);
		await ApplicationContext.SaveChangesAsync();
	}
}
