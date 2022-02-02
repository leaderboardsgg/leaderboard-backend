using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public class CategoryService : ICategoryService
	{
		private ApplicationContext _applicationContext;
		public CategoryService(ApplicationContext applicationContext)
		{
			_applicationContext = applicationContext;
		}

		public async Task<Category?> GetCategory(ulong id)
		{
			return await _applicationContext.Categories.FindAsync(id);
		}
	}
}
