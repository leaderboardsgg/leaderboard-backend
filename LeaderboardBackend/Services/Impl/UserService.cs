using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Services;

public class UserService : IUserService
{
    private readonly ApplicationContext _applicationContext;

    public UserService(ApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
    }

    // TODO: Convert return sig to Task<GetUserResult>
    public async Task<User?> GetUserById(Guid id)
    {
        return await _applicationContext.Users.FindAsync(id);
    }

    // TODO: Convert return sig to Task<GetUserResult>
    public async Task<User?> GetUserByEmail(string email)
    {
        return await _applicationContext.Users.SingleOrDefaultAsync(user => user.Email == email);
    }

    public async Task<GetUserResult> GetUserByEmailAndPassword(string email, string password)
    {
        User? user = await _applicationContext.Users.SingleOrDefaultAsync(user => user.Email == email);

        if (user is null)
        {
            return new GetUserErrors(NotFound: true);
        }

        if (user.Role is UserRole.Banned)
        {
            return new GetUserErrors(Banned: true);
        }

        if (!BCryptNet.EnhancedVerify(password, user.Password))
        {
            return new GetUserErrors(Unauthorised: true);
        }

        return user;
    }

    // TODO: Convert return sig to Task<GetUserResult>
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
