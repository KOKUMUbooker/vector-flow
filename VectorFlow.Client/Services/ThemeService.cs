using Microsoft.JSInterop;

namespace VectorFlow.Client.Services;

/// <summary>
/// Manages light/dark theme state for the app.
///
/// Priority order on startup:
///   1. If user has previously chosen a theme manually → honour that choice.
///   2. If user has never touched the toggle → read the OS preference.
///   3. If everything fails → default to light mode.
///
/// Stored keys in localStorage:
///   "vf:useSystemPreference"  bool  — whether to follow the OS
///   "vf:isDarkMode"           bool  — the manually chosen value (only used when above is false)
/// </summary>
public class ThemeService(
    ICustomLocalStorageService localStorage,
    IJSRuntime jsRuntime)
{
    // Prefix all keys to avoid collisions with other libs using localStorage
    private const string KeyUseSystem = "vf:useSystemPreference";
    private const string KeyIsDark = "vf:isDarkMode";

    public event Action? OnThemeChanged;

    private bool _isDarkMode;
    private bool _useSystemPreference = true; // default before init

    // ── Public state ──────────────────────────────────────────────────────────

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode == value) return;
            _isDarkMode = value;
            _useSystemPreference = false; // user made a manual choice
            OnThemeChanged?.Invoke();
            _ = PersistAsync();
        }
    }

    public bool UseSystemPreference
    {
        get => _useSystemPreference;
        set
        {
            if (_useSystemPreference == value) return;
            _useSystemPreference = value;
            if (value) _ = ApplySystemThemeAsync();
            OnThemeChanged?.Invoke();
            _ = PersistAsync();
        }
    }

    // ── Initialisation (called once from MainLayout.OnInitializedAsync) ───────

    public async Task InitializeAsync()
    {
        try
        {
            // Check if the user has previously saved a preference
            var savedUseSystem = await localStorage.GetItemAsync<bool?>(KeyUseSystem);

            if (savedUseSystem is null)
            {
                // First visit — no saved preference at all, follow the OS
                _useSystemPreference = true;
                await ApplySystemThemeAsync();
            }
            else if (savedUseSystem == true)
            {
                _useSystemPreference = true;
                await ApplySystemThemeAsync();
            }
            else
            {
                // User previously made a manual choice
                _useSystemPreference = false;
                _isDarkMode = await localStorage.GetItemAsync<bool>(KeyIsDark);
            }
        }
        catch
        {
            // Last resort fallback — light mode
            _useSystemPreference = true;
            _isDarkMode = false;
        }

        OnThemeChanged?.Invoke();
    }

    // ── Toggle helper (used by the navbar button) ─────────────────────────────

    public void Toggle() => IsDarkMode = !IsDarkMode;

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Reads the OS colour scheme preference via the standard
    /// CSS media query API — no custom JS function required.
    /// </summary>
    private async Task ApplySystemThemeAsync()
    {
        try
        {
            _isDarkMode = await jsRuntime.InvokeAsync<bool>(
                "eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
        }
        catch
        {
            _isDarkMode = false;
        }
    }

    private async Task PersistAsync()
    {
        await localStorage.SetItemAsync(KeyUseSystem, _useSystemPreference);
        // Always persist the current value so we can restore it
        // if the user later switches back from system → manual
        await localStorage.SetItemAsync(KeyIsDark, _isDarkMode);
    }
}