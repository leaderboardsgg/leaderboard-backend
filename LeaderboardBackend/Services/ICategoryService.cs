using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface ICategoryService
{
    Task<Category?> GetCategory(long id);
    Task CreateCategory(Category category);
    Task<Category?> GetCategoryForRun(Run run);
}
