using System.Net;

namespace VectorFlow.Client.Services;

/// <summary>
/// DelegatingHandler that sits on every outgoing HttpClient request.
///
/// If a request returns 401 Unauthorized, it attempts a silent token refresh
/// by calling POST /api/auth/refresh (the refresh token cookie is sent
/// automatically). If the refresh succeeds, it retries the original request.
/// If the refresh also fails, it calls NotifyLoggedOut() so Blazor redirects
/// the user to the login page.
/// </summary>
public class RefreshTokenHandler(
    VectorFlowAuthStateProvider authStateProvider) : DelegatingHandler
{
    // Prevent recursive refresh — if the /refresh call itself gets a 401
    // we don't want to try refreshing again forever.
    private bool _isRefreshing;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        // Only intercept 401s — and not if we're already in a refresh attempt
        if (response.StatusCode != HttpStatusCode.Unauthorized || _isRefreshing)
            return response;

        // Don't try to refresh if this IS the refresh or login call —
        // that would cause an infinite loop
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;
        if (path.Contains("/auth/refresh") || path.Contains("/auth/login"))
            return response;

        _isRefreshing = true;

        try
        {
            var refreshResponse = await base.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh"),
                cancellationToken);

            if (refreshResponse.IsSuccessStatusCode)
            {
                // New cookies are set by the server — retry the original request.
                // Must clone the request because HttpRequestMessage can't be sent twice.
                var retryRequest = await CloneRequestAsync(request);
                response = await base.SendAsync(retryRequest, cancellationToken);
            }
            else
            {
                // Refresh token is expired or revoked — log the user out
                authStateProvider.NotifyLoggedOut();
            }
        }
        finally
        {
            _isRefreshing = false;
        }

        return response;
    }

    /// <summary>
    /// HttpRequestMessage can't be re-sent after it's been read.
    /// This creates a fresh copy with the same method, URI, headers, and body.
    /// </summary>
    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        if (original.Content is not null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);

            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}