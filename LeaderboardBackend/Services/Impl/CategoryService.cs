using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public class CategoryService : ICategoryService
{
	private readonly ApplicationContext ApplicationContext;

	public CategoryService(ApplicationContext applicationContext)
	{
		ApplicationContext = applicationContext;
	}

	public async Task<Category?> GetCategory(long id)
	{
		return await ApplicationContext.Categories.FindAsync(id);
	}

	public async Task CreateCategory(Category category)
	{
		ApplicationContext.Categories.Add(category);
		await ApplicationContext.SaveChangesAsync();
	}
}
