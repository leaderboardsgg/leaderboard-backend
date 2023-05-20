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
        return await _applicationContext.Users.FirstOrDefaultAsync(user => user.Email == email);
    }

    public async Task<User?> GetUserByName(string name)
    {
        string lowerName = name.ToLower();

        // We save a username with casing, but match without.
        // Effectively you can't have two separate users named e.g. "cool" and "cOoL".
        return await _applicationContext.Users.FirstOrDefaultAsync(
            user => user.Username != null && user.Username.ToLower() == lowerName
        );
    }

    public async Task<CreateUserResult> CreateUser(RegisterRequest request)
    {
        /* Linq query translated as:
        SELECT count(*) FILTER (WHERE lower(t.username) = @__ToLower_0)::int > 0 AS "usernameExists",
            count(*) FILTER (WHERE lower(t.email) = @__ToLower_1)::int > 0 AS "emailExists"
        FROM (
            SELECT u.email, u.username, 1 AS "Key"
            FROM users AS u
        ) AS t
        GROUP BY t."Key"
        LIMIT 1
        */
        var conflicts = await _applicationContext.Users.GroupBy(x => 1)
            .Select(users => new
            {
                usernameExists = users.Count(x => x.Username.ToLower() == request.Username.ToLower()) > 0,
                emailExists = users.Count(x => x.Email.ToLower() == request.Email.ToLower()) > 0
            }).FirstOrDefaultAsync();

        if (conflicts is not null && (conflicts.usernameExists || conflicts.emailExists))
        {
            return new CreateUserConflicts(Username: conflicts.usernameExists, Email: conflicts.emailExists);
        }

        User newUser =
            new()
            {
                Username = request.Username,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password)
            };

        _applicationContext.Users.Add(newUser);
        await _applicationContext.SaveChangesAsync();

        return newUser;
    }
}
