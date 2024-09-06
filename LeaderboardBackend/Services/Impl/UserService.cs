using System.Security.Claims;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Npgsql;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Services;

public class UserService(ApplicationContext applicationContext, IAuthService authService) : IUserService
{
    // TODO: Convert return sig to Task<GetUserResult>
    public async Task<User?> GetUserById(Guid id)
    {
        return await applicationContext.Users.FindAsync(id);
    }

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
    {
        return await applicationContext.Users.SingleOrDefaultAsync(user => user.Email == email);
    }

    public async Task<LoginResult> LoginByEmailAndPassword(string email, string password)
    {
        User? user = await applicationContext.Users.SingleOrDefaultAsync(user => user.Email == email);

        if (user is null)
        {
            return new UserNotFound();
        }

        if (user.Role is UserRole.Banned)
        {
            return new UserBanned();
        }

        if (!BCryptNet.EnhancedVerify(password, user.Password))
        {
            return new BadCredentials();
        }

        return authService.GenerateJSONWebToken(user);
    }

    // TODO: Convert return sig to Task<GetUserResult>
    public async Task<User?> GetUserByName(string name)
    {
        return await applicationContext.Users.SingleOrDefaultAsync(user => user.Username == name);
    }

    public async Task<User?> GetUserByNameAndEmail(string name, string email)
    {
        return await applicationContext.Users.SingleOrDefaultAsync(
            user => user.Username == name && user.Email == email
        );
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
}
