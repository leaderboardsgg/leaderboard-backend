using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface ICategoryService : IBaseService<Category, long>
{
	Task<List<Category>> GetCategories(long[] ids);
	Task<Category?> GetCategoryForRun(Run run);
}
