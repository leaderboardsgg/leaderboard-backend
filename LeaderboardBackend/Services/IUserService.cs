using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using OneOf;

namespace LeaderboardBackend.Services;

public interface IUserService
{
    // TODO: Convert return sig to Task<GetUserResult>
    Task<User?> GetUserById(Guid id);
    // TODO: Convert return sig to Task<GetUserResult>
    Task<User?> GetUserByEmail(string email);
    Task<LoginResult> LoginByEmailAndPassword(string email, string password);
    // TODO: Convert return sig to Task<GetUserResult>
    Task<User?> GetUserByName(string name);
    Task<CreateUserResult> CreateUser(RegisterRequest request);

}

[GenerateOneOf]
public partial class CreateUserResult : OneOfBase<User, CreateUserConflicts> { }

[GenerateOneOf]
public partial class LoginResult : OneOfBase<string, LoginErrors> { }

public readonly record struct CreateUserConflicts(bool Username = false, bool Email = false);
public readonly record struct LoginErrors(bool NotFound = false, bool Banned = false, bool WrongPassword = false);
