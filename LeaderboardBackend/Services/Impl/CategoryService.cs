using LeaderboardBackend.Models;
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

    public async Task<Category?> GetCategoryBySlug(long leaderboardId, string slug) =>
        await applicationContext.Categories
            .FirstOrDefaultAsync(c => c.Slug == slug && c.LeaderboardId == leaderboardId && c.DeletedAt == null);

    public async Task<GetCategoriesForLeaderboardResult> GetCategoriesForLeaderboard(long leaderboardId, StatusFilter statusFilter, Page page)
    {
        Leaderboard? board = await applicationContext.Leaderboards
            .Where(board => board.Id == leaderboardId)
            .Include(board => board.Categories)
            .SingleOrDefaultAsync();

        if (board is null)
        {
            return new NotFound();
        }

        IEnumerable<Category> cats = board.Categories!.FilterByStatus(statusFilter);
        long count = cats.TryGetNonEnumeratedCount(out int countFast) ? countFast : cats.LongCount();
        List<Category> items = cats.OrderBy(cat => cat.Id).Skip(page.Offset).Take(page.Limit).ToList();
        return new ListResult<Category>(items, count);
    }

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

    public async Task<UpdateResult<Category>> UpdateCategory(long id, UpdateCategoryRequest request)
    {
        Category? cat = await applicationContext.Categories.FindAsync(id);

        if (cat is null)
        {
            return new NotFound();
        }

        if (request.Name is not null)
        {
            cat.Name = request.Name;
        }

        if (request.Slug is not null)
        {
            cat.Slug = request.Slug;
        }

        if (request.Info is not null)
        {
            cat.Info = request.Info;
        }

        if (request.SortDirection is not null)
        {
            cat.SortDirection = (SortDirection)request.SortDirection;
        }

        switch (request.Status)
        {
            case null:
                break;

            case Status.Published:
            {
                cat.DeletedAt = null;
                break;
            }

            case Status.Deleted:
            {
                cat.DeletedAt = clock.GetCurrentInstant();
                break;
            }

            default:
                throw new ArgumentException($"Invalid Status in request: {(int)request.Status}", nameof(request));
        }

        try
        {
            await applicationContext.SaveChangesAsync();
        }
        catch (DbUpdateException e)
            when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            Category conflict = await applicationContext.Categories.SingleAsync(c => c.Slug == cat.Slug && c.LeaderboardId == cat.LeaderboardId && c.DeletedAt == null);
            return new Conflict<Category>(conflict);
        }

        return new Success();
    }

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
            Category conflict = await applicationContext.Categories.SingleAsync(c => c.Slug == cat.Slug && c.DeletedAt == null && c.LeaderboardId == cat.LeaderboardId);
            return new Conflict<Category>(conflict);
        }

        return cat;
    }
}
