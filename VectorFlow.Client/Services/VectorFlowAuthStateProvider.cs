using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using VectorFlow.Shared.DTOs;

namespace VectorFlow.Client.Services;

/// <summary>
/// Tells Blazor who the current user is.
///
/// On every app start (or page reload), Blazor calls GetAuthenticationStateAsync().
/// We hit GET /api/auth/me — the HttpOnly access token cookie is sent automatically
/// by the browser, so the API can validate it and return user details.
///
/// After login/logout, ClientAuthService calls NotifyLoggedIn / NotifyLoggedOut
/// which updates _cachedUser and fires NotifyAuthenticationStateChanged() —
/// Blazor then re-calls GetAuthenticationStateAsync() to get the new state.
/// The cache means login/logout don't need another API round-trip.
/// </summary>
public class VectorFlowAuthStateProvider(IHttpClientFactory httpClientFactory)
    : AuthenticationStateProvider
{
    // Cached after a successful /me call or login.
    // Null means anonymous or not yet initialised.
    private UserDto? _cachedUser;

    // Create the client lazily — only when actually making a request.
    // This breaks the circular dependency because the factory itself
    // doesn't trigger the handler pipeline until CreateClient() is called,
    // and by then VectorFlowAuthStateProvider is already fully constructed.
    private HttpClient Http => httpClientFactory.CreateClient("VectorFlowApi");

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // If we already have the user (e.g. just logged in), return immediately
        // without hitting the API again.
        if (_cachedUser is not null)
            return BuildState(_cachedUser);

        try
        {
            // Cookie is sent automatically — no manual token handling needed.
            var user = await Http.GetFromJsonAsync<UserDto>("api/auth/me");

            if (user is null)
                return Anonymous();

            _cachedUser = user;
            return BuildState(user);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Access token expired or missing — RefreshTokenHandler will have
            // already tried to refresh. If we're here, both tokens are gone.
            return Anonymous();
        }
        catch
        {
            // Network error or server down — treat as anonymous
            return Anonymous();
        }
    }

    /// <summary>
    /// Called by ClientAuthService immediately after a successful login.
    /// Caches the user and notifies Blazor to re-render auth-sensitive UI.
    /// </summary>
    public void NotifyLoggedIn(UserDto user)
    {
        _cachedUser = user;
        NotifyAuthenticationStateChanged(
            Task.FromResult(BuildState(user)));
    }

    /// <summary>
    /// Called by ClientAuthService after logout.
    /// Clears the cache and notifies Blazor — protected routes will redirect.
    /// </summary>
    public void NotifyLoggedOut()
    {
        _cachedUser = null;
        NotifyAuthenticationStateChanged(
            Task.FromResult(Anonymous()));
    }

    /// <summary>Exposes the cached user for components that need it directly.</summary>
    public UserDto? GetCachedUser() => _cachedUser;

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AuthenticationState BuildState(UserDto user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Name,           user.DisplayName),
        };

        // The second argument to ClaimsIdentity is the authentication type.
        // Any non-null, non-empty string means "authenticated" to Blazor.
        var identity = new ClaimsIdentity(claims, authenticationType: "cookie");
        var principal = new ClaimsPrincipal(identity);

        return new AuthenticationState(principal);
    }

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}