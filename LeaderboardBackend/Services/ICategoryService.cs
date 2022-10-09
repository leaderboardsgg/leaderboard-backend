using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface ICategoryService
{
	Task<Category?> GetCategory(long id);
	Task<List<Category>> GetCategories(long[] ids);
	Task CreateCategory(Category category);
	Task<Category?> GetCategoryForRun(Run run);
}
