using VectorFlow.Shared.DTOs;

namespace VectorFlow.Client.Services.Interfaces;

public interface IClientAuthService
{
    Task<UILoginResult> LoginAsync(LoginRequest request);
    Task<RegisterResult> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<UserDto?> GetCurrentUserAsync();
}

public class UILoginResult
{
    public bool Succeeded { get; set; }
    public bool EmailNotVerified { get; set; }
    public string? Error { get; set; }
    public UserDto? User { get; set; }

    public static UILoginResult Success(UserDto user) =>
        new() { Succeeded = true, User = user };
    public static UILoginResult Unverified() =>
        new() { EmailNotVerified = true };
    public static UILoginResult Failure(string error) =>
        new() { Error = error };
}