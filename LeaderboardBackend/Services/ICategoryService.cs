using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public interface ICategoryService
	{
		Task<Category?> GetCategory(ulong id);
	}
}
