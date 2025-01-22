using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Npgsql;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public class CategoryService(ApplicationContext applicationContext, IClock clock) : ICategoryService
{
    public async Task<Category?> GetCategory(long id) =>
        await applicationContext.Categories.FindAsync(id);

    public async Task<CreateCategoryResult> CreateCategory(long leaderboardId, CreateCategoryRequest request)
    {
        Category category =
            new()
            {
                Name = request.Name,
                Slug = request.Slug,
                Info = request.Info,
                LeaderboardId = leaderboardId,
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
            Category conflict = await applicationContext.Categories.SingleAsync(c => c.Slug == category.Slug && c.DeletedAt == null);
            return new Conflict<Category>(conflict);
        }

        return category;
    }

    public async Task<Category?> GetCategoryForRun(Run run) =>
        await applicationContext.Categories.FindAsync(run.CategoryId);

    public async Task<DeleteResult> DeleteCategory(long id)
    {
        Category? category = await applicationContext.Categories.FindAsync(id);

        if (category == null)
        {
            return new NotFound();
        }

        if (category.DeletedAt != null)
        {
            return new AlreadyDeleted();
        }

        category.DeletedAt = clock.GetCurrentInstant();
        await applicationContext.SaveChangesAsync();
        return new Success();
    }

    public async Task<RestoreResult<Category>> RestoreCategory(long id)
    {
        Category? cat = await applicationContext.Categories.FindAsync(id);

        if (cat == null)
        {
            return new NotFound();
        }

        if (cat.DeletedAt == null)
        {
            return new NeverDeleted();
        }

        cat.DeletedAt = null;

        try
        {
            await applicationContext.SaveChangesAsync();
        }
        catch (DbUpdateException e)
            when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pgEx)
        {
            Category conflict = await applicationContext.Categories.SingleAsync(c => c.Slug == cat.Slug && c.DeletedAt == null);
            return new Conflict<Category>(conflict);
        }

        return cat;
    }
}
