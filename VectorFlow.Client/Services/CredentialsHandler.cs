using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace VectorFlow.Client.Services;

public class CredentialsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Include);

        return base.SendAsync(request, cancellationToken);
    }
}