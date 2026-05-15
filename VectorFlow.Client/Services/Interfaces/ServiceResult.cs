using System.Net.Http.Json;

namespace VectorFlow.Client.Services.Interfaces;

/// <summary>
/// Generic result type used by service methods.
/// Avoids throwing exceptions for expected failure cases (404, 403, validation errors)
/// so the UI can branch cleanly without try/catch everywhere.
/// </summary>
public class ServiceResult<T>
{
    public bool Succeeded { get; protected set; }
    public T? Data { get; protected set; }
    public string? Error { get; protected set; }
    public bool NotFound { get; protected set; }
    public bool Forbidden { get; protected set; }

    public static ServiceResult<T> Success(T data) =>
        new() { Succeeded = true, Data = data };

    public static ServiceResult<T> Failure(string error) =>
        new() { Error = error };

    public static ServiceResult<T> NotFoundResult(string? name) =>
        new() { NotFound = true, Error = $"{name ?? "Item"} not found." };

    public static ServiceResult<T> ForbiddenResult() =>
        new() { Forbidden = true, Error = "You don't have permission to perform this action." };


}

// Convenience alias for void-like results
public class ServiceResult : ServiceResult<bool>
{
    public static ServiceResult Ok() =>
        new() { Succeeded = true, Data = true };

    public new static ServiceResult NotFoundResult(string? name) =>
    new() { NotFound = true, Error = $"{name ?? "Item"} not found." };

    public new static ServiceResult Failure(string error) =>
        new() { Error = error };

    public new static ServiceResult ForbiddenResult() =>
        new() { Forbidden = true, Error = "You don't have permission to perform this action." };
}

public static class ErrorUtil {
    // ── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Tries to read a { "message": "..." } body from an error response.
    /// Falls back to a generic message if the body can't be parsed.
    /// </summary>
    public static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            return body?.Message ?? "An unexpected error occurred.";
        }
        catch
        {
            return "An unexpected error occurred.";
        }
    }

    public record ErrorBody(string? Message);
}

public class MessageRes
{
     public string Message { get; set; } = string.Empty;
}