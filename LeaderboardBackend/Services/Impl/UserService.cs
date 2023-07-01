using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using Microsoft.EntityFrameworkCore;

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
        /* Linq query translated as:
        SELECT count(*) FILTER (WHERE t.username = @__request_Username_0)::int > 0 AS "usernameExists",
            count(*) FILTER (WHERE t.email = @__request_Email_1)::int > 0 AS "emailExists"
        FROM (
            SELECT u.email, u.username, 1 AS "Key"
            FROM users AS u
        ) AS t
        GROUP BY t."Key"
        LIMIT 2
        */
        var conflicts = await _applicationContext.Users.GroupBy(x => 1)
            .Select(users => new
            {
                usernameExists = users.Count(x => x.Username == request.Username) > 0,
                emailExists = users.Count(x => x.Email == request.Email) > 0
            }).SingleAsync();

        if (conflicts.usernameExists || conflicts.emailExists)
        {
            return new CreateUserConflicts(Username: conflicts.usernameExists, Email: conflicts.emailExists);
        }

        User newUser =
            new()
            {
                Username = request.Username,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password),
                Role = UserRole.Registered
            };

        _applicationContext.Users.Add(newUser);
        await _applicationContext.SaveChangesAsync();

        return newUser;
    }
}
