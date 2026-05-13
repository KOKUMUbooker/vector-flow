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