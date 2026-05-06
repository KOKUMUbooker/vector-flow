using Microsoft.JSInterop;
using System.Text.Json;

namespace VectorFlow.Client.Services;

public interface ICustomLocalStorageService
{
    Task SetItemAsync<T>(string key, T value);
    Task<T?> GetItemAsync<T>(string key);
    Task RemoveItemAsync(string key);
    Task ClearAsync();
}

public class CustomLocalStorageService : ICustomLocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    
    public CustomLocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task SetItemAsync<T>(string key, T value)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, serializedValue);
    }
    
    public async Task<T?> GetItemAsync<T>(string key)
    {
        try
        {
            var serializedValue = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            if (string.IsNullOrEmpty(serializedValue))
                return default;
            
            return JsonSerializer.Deserialize<T>(serializedValue);
        }
        catch
        {
            return default;
        }
    }
    
    public async Task RemoveItemAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }
    
    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.clear");
    }
}