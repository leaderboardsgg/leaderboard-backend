using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services
{
	public class CategoryService : ICategoryService
	{
		private readonly ApplicationContext _applicationContext;

		public CategoryService(ApplicationContext applicationContext)
		{
			_applicationContext = applicationContext;
		}

		public async Task<Category?> GetCategory(long id)
		{
			return await _applicationContext.Categories.FindAsync(id);
		}

		public async Task CreateCategory(Category category)
		{
			_applicationContext.Categories.Add(category);
			await _applicationContext.SaveChangesAsync();
		}
	}
}
