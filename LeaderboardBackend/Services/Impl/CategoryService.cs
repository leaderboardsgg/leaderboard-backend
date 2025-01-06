using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public class CategoryService(ApplicationContext applicationContext) : ICategoryService
{
    public async Task<Category?> GetCategory(long id) =>
        await applicationContext.Categories.FindAsync(id);

    public async Task<CreateCategoryResult> CreateCategory(CreateCategoryRequest request)
    {
        Category category =
            new()
            {
                Name = request.Name,
                Slug = request.Slug,
                Info = request.Info,
                LeaderboardId = request.LeaderboardId,
                SortDirection = request.SortDirection,
                Type = request.Type
            };

        applicationContext.Categories.Add(category);

        try
        {
            await applicationContext.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return new NotFound();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pgEx)
        {
            return new Conflict<Category>();
        }

        return category;
    }

    public async Task<Category?> GetCategoryForRun(Run run) =>
        await applicationContext.Categories.FindAsync(run.CategoryId);
}
