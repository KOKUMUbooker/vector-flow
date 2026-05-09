// VectorFlow.Client/Services/ClientAuthService.cs
using System.Net;
using System.Net.Http.Json;
using VectorFlow.Client.Services.Interfaces;
using VectorFlow.Shared.DTOs;

namespace VectorFlow.Client.Services;

public class ClientAuthService(
     IHttpClientFactory httpClientFactory,
    VectorFlowAuthStateProvider authStateProvider) : IClientAuthService
{
    private HttpClient Http => httpClientFactory.CreateClient("VectorFlowApi");

    public async Task<UILoginResult> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await Http.PostAsJsonAsync("api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserDto>();
                authStateProvider.NotifyLoggedIn(user!);
                return UILoginResult.Success(user!);
            }

            // 403 = correct credentials but email not yet verified
            if (response.StatusCode == HttpStatusCode.Forbidden)
                return UILoginResult.Unverified();

            // 401 = wrong credentials
            return UILoginResult.Failure("Invalid email or password.");
        }
        catch
        {
            return UILoginResult.Failure("Unable to reach the server. Check your connection.");
        }
    }

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await Http.PostAsJsonAsync("api/auth/register", request);

            if (response.IsSuccessStatusCode)
                return RegisterResult.Success();

            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            var errors = body?.Errors ?? ["Registration failed."];
            return RegisterResult.Failure(errors);
        }
        catch
        {
            return RegisterResult.Failure(["Unable to reach the server."]);
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await Http.PostAsync("api/auth/logout", null);
        }
        finally
        {
            // Always clear local auth state even if the server call fails
            authStateProvider.NotifyLoggedOut();
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            return await Http.GetFromJsonAsync<UserDto>("api/auth/me");
        }
        catch
        {
            return null;
        }
    }

    private record ErrorBody(IEnumerable<string> Errors);
}