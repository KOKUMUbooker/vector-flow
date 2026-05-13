using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using VectorFlow.Client.Services;
using VectorFlow.Client.Services.Interfaces;

namespace VectorFlow.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // ── Auth state ──────────────────────────────────────────────────────────
        // Scoped so every component tree gets the same instance within a circuit.
        builder.Services.AddScoped<VectorFlowAuthStateProvider>();

        // Register as the base class — Blazor resolves it by the base type.
        builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<VectorFlowAuthStateProvider>());

        builder.Services.AddAuthorizationCore();

        // ── HTTP client ─────────────────────────────────────────────────────────
        // RefreshTokenHandler needs VectorFlowAuthStateProvider injected,
        // so register it as Scoped (not Transient).
        builder.Services.AddTransient<CredentialsHandler>();
        builder.Services.AddScoped<RefreshTokenHandler>();
       
        builder.Services.AddHttpClient("VectorFlowApi", client =>
        {
            // Use "Constants.Constants.ApiBaseUrl" when running UI and api separately
            // var url =  builder.HostEnvironment.IsDevelopment() ? Constants.Constants.ApiBaseUrl : builder.HostEnvironment.BaseAddress;
            client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
        })
        .AddHttpMessageHandler<CredentialsHandler>() // Comes first - ensured cookies get attached on every request
        .AddHttpMessageHandler<RefreshTokenHandler>();

        builder.Services.AddMudServices();
        builder.Services.AddScoped<IClientAuthService, ClientAuthService>();
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddScoped<IInvitationService, InvitationService>();
        builder.Services.AddScoped<ThemeService>();
        builder.Services.AddScoped<ICustomLocalStorageService,CustomLocalStorageService>();

        await builder.Build().RunAsync();
    }
}
