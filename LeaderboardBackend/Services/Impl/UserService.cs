using System.Security.Claims;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OneOf.Types;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Services;

public class UserService(ApplicationContext applicationContext, IAuthService authService) : IUserService
{
    // TODO: Convert return sig to Task<GetUserResult>
    public async Task<User?> GetUserById(Guid id) =>
        await applicationContext.Users.FindAsync(id);

    public async Task<GetUserResult> GetUserFromClaims(ClaimsPrincipal claims)
    {
        Guid? id = authService.GetUserIdFromClaims(claims);

        if (id is null)
        {
            return new BadCredentials();
        }

        User? user = await applicationContext.Users.FindAsync(id);

        if (user is null)
        {
            return new UserNotFound();
        }

        return user;
    }

    // TODO: Convert return sig to Task<GetUserResult>
    public async Task<User?> GetUserByEmail(string email)
        => await applicationContext.Users.SingleOrDefaultAsync(user => user.Email == email);

    public async Task<LoginResult> LoginByEmailAndPassword(string email, string password)
    {
        User? user = await applicationContext.Users.SingleOrDefaultAsync(user => user.Email == email);

        if (user is null)
        {
            return new UserNotFound();
        }

        if (!BCryptNet.EnhancedVerify(password, user.Password))
        {
            return new BadCredentials();
        }

        if (user.Role is UserRole.Banned)
        {
            return new UserBanned();
        }

        return authService.GenerateJSONWebToken(user);
    }

    // TODO: Convert return sig to Task<GetUserResult>
    public async Task<User?> GetUserByName(string name) =>
        await applicationContext.Users.SingleOrDefaultAsync(user => user.Username == name);

    public async Task<User?> GetUserByNameAndEmail(string name, string email)
        => await applicationContext.Users.SingleOrDefaultAsync(
            user => user.Username == name && user.Email == email
        );

    public async Task<ListResult<User>> ListUsers(Page page, UserStatusFilter statusFilter)
    {
        IQueryable<User> query = applicationContext.Users.FilterByUserStatus(statusFilter);
        long count = await query.LongCountAsync();

        List<User> items = await query
            .Skip(page.Offset)
            .Take(page.Limit)
            .ToListAsync();
        return new ListResult<User>(items, count);
    }

    public async Task<CreateUserResult> CreateUser(RegisterRequest request)
    {
        User newUser =
            new()
            {
                Username = request.Username,
                Email = request.Email,
                Password = BCryptNet.EnhancedHashPassword(request.Password),
                Role = UserRole.Registered,
            };

        applicationContext.Users.Add(newUser);

        try
        {
            await applicationContext.SaveChangesAsync();
        }
        catch (DbUpdateException e)
            when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pgEx)
        {
            return pgEx.ConstraintName switch
            {
                UserEntityTypeConfig.USERNAME_UNIQUE_INDEX => new CreateUserConflicts(Username: true),
                UserEntityTypeConfig.EMAIL_UNIQUE_INDEX => new CreateUserConflicts(Email: true),
                _ => throw new NotImplementedException($"Violation of {pgEx.ConstraintName} constraint is not handled", pgEx)
            };
        }

        return newUser;
    }

    public async Task<UpdateUserResult> UpdateUser(Guid id, UpdateUserRequest request)
    {
        User? user = await applicationContext.Users.SingleOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return new UserNotFound();
        }

        if (user.Role == UserRole.Administrator)
        {
            return new BadRole();
        }

        if (request.Role == UserRole.Administrator || request.Role == UserRole.Registered)
        {
            return new RoleChangeForbidden();
        }

        user.Role = request.Role;
        await applicationContext.SaveChangesAsync();
        return new Success();
    }
}
