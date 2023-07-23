using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using OneOf;

namespace LeaderboardBackend.Services;

public interface IUserService
{
    Task<User?> GetUserById(Guid id);
    Task<User?> GetUserByEmail(string email);
    Task<User?> GetUserByName(string name);
    Task<CreateUserResult> CreateUser(RegisterRequest request);

}

[GenerateOneOf]
public partial class CreateUserResult : OneOfBase<User, CreateUserConflicts> { }

public readonly record struct CreateUserConflicts(bool Username = false, bool Email = false);

