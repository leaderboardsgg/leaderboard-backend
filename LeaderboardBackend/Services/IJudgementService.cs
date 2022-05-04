using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IJudgementService
{
	Task<Judgement?> GetJudgement(long id);
	Task CreateJudgement(Judgement judgement);
}
