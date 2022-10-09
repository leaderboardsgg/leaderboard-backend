using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class CategoryService : ICategoryService
{
	private readonly ApplicationContext _applicationContext;

	public CategoryService(ApplicationContext applicationContext)
	{
		_applicationContext = applicationContext;
	}

	public async Task<Category?> GetCategory(long id)
	{
		return await _applicationContext.Categories
			.FindAsync(id);
	}

	public async Task<List<Category>> GetCategories(long[] ids)
	{
		return await _applicationContext.Categories.Where(cat => ids.Contains(cat.Id)).ToListAsync();
	}

	public async Task CreateCategory(Category category)
	{
		_applicationContext.Categories.Add(category);
		await _applicationContext.SaveChangesAsync();
	}

	public async Task<Category?> GetCategoryForRun(Run run)
	{
		return await _applicationContext.Categories.FindAsync(run.CategoryId);
	}
}
