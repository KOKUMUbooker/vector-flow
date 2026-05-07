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

public class CustomLocalStorageService(IJSRuntime jsRuntime) : ICustomLocalStorageService
{
    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        try
        {
            var json = await jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    public async Task RemoveItemAsync(string key)
        => await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);

    public async Task ClearAsync()
        => await jsRuntime.InvokeVoidAsync("localStorage.clear");
}