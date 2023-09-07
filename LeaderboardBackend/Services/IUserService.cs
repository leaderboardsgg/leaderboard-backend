using System.Security.Claims;
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
    Task<GetUserResult> GetUserFromClaims(ClaimsPrincipal claims);
    Task<LoginResult> LoginByEmailAndPassword(string email, string password);
    // TODO: Convert return sig to Task<GetUserResult>
    Task<User?> GetUserByName(string name);
    Task<CreateUserResult> CreateUser(RegisterRequest request);

}

[GenerateOneOf]
public partial class CreateUserResult : OneOfBase<User, CreateUserConflicts> { }

public readonly record struct CreateUserConflicts(bool Username = false, bool Email = false);

[GenerateOneOf]
public partial class LoginResult : OneOfBase<string, UserNotFound, UserBanned, BadCredentials> { }

public readonly record struct UserNotFound();
public readonly record struct UserBanned();
public readonly record struct BadCredentials();

[GenerateOneOf]
public partial class GetUserResult : OneOfBase<User, BadCredentials, UserNotFound> { }
