namespace SMS.Application.Common.Models;

/// <summary>
/// Generic result wrapper for operation outcomes
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? Error { get; }
    public IEnumerable<string> Errors { get; }

    private Result(bool isSuccess, T? data, string? error, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
        Errors = errors ?? [];
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    public static Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.FirstOrDefault(), errors);
}

/// <summary>
/// Non-generic result wrapper for operations without return data
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public IEnumerable<string> Errors { get; }

    private Result(bool isSuccess, string? error, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? [];
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(IEnumerable<string> errors) => new(false, errors.FirstOrDefault(), errors);
}
