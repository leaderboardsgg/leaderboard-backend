using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LeaderboardBackend.Services;

public class UserService : IUserService
{
    private readonly ApplicationContext _applicationContext;

    public UserService(ApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
    }

    public async Task<User?> GetUserById(Guid id)
    {
        return await _applicationContext.Users.FindAsync(id);
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        return await _applicationContext.Users.SingleOrDefaultAsync(user => user.Email == email);
    }

    public async Task<User?> GetUserByName(string name)
    {
        return await _applicationContext.Users.SingleOrDefaultAsync(user => user.Username == name);
    }

    public async Task<CreateUserResult> CreateUser(RegisterRequest request)
    {
        User newUser =
            new()
            {
                Username = request.Username,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password),
                Role = UserRole.Registered
            };

        _applicationContext.Users.Add(newUser);

        try
        {
            await _applicationContext.SaveChangesAsync();
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
