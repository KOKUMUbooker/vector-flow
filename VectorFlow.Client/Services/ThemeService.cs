using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace VectorFlow.Client.Services;

public class ThemeService
{
    private readonly ICustomLocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;
    
    public event Action? OnThemeChanged;
    
    private bool _isDarkMode;
    private bool _useSystemPreference = true;
    
    public ThemeService(ICustomLocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
    }
    
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                _useSystemPreference = false;
                OnThemeChanged?.Invoke();
                _ = SaveThemePreference();
            }
        }
    }
    
    public bool UseSystemPreference
    {
        get => _useSystemPreference;
        set
        {
            if (_useSystemPreference != value)
            {
                _useSystemPreference = value;
                if (value)
                {
                    _ = DetectSystemTheme();
                }
                OnThemeChanged?.Invoke();
                _ = SaveThemePreference();
            }
        }
    }
    
    public async Task InitializeThemeAsync()
    {
        try
        {
            var savedUseSystem = await _localStorage.GetItemAsync<bool>("useSystemPreference");
            _useSystemPreference = savedUseSystem;
            
            if (_useSystemPreference)
            {
                await DetectSystemTheme();
            }
            else
            {
                var savedTheme = await _localStorage.GetItemAsync<bool>("themePreference");
                _isDarkMode = savedTheme;
            }
            
            OnThemeChanged?.Invoke();
        }
        catch
        {
            await DetectSystemTheme();
        }
    }
    
    private async Task DetectSystemTheme()
    {
        try
        {
            // Use a more reliable approach
            var systemDarkMode = await _jsRuntime.InvokeAsync<bool>("window.getSystemTheme");
            _isDarkMode = systemDarkMode;
        }
        catch
        {
            // Fallback to checking via matchMedia
            try
            {
                var result = await _jsRuntime.InvokeAsync<bool>("eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
                _isDarkMode = result;
            }
            catch
            {
                _isDarkMode = false; // Default to light mode
            }
        }
    }
    
    private async Task SaveThemePreference()
    {
        await _localStorage.SetItemAsync("useSystemPreference", _useSystemPreference);
        if (!_useSystemPreference)
        {
            await _localStorage.SetItemAsync("themePreference", _isDarkMode);
        }
    }
    
    public async Task ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
    }
    
    public async Task SetUseSystemPreference(bool useSystem)
    {
        UseSystemPreference = useSystem;
    }
}
