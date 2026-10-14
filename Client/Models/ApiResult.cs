namespace Client.Models;

/// <summary>Результат вызова API без полезной нагрузки.</summary>
public class ApiResult
{
    public bool Success { get; init; }

    public IReadOnlyDictionary<string, string[]> Errors { get; init; } =
        new Dictionary<string, string[]>();

    public static ApiResult Ok() => new() { Success = true };

    public static ApiResult Fail(IReadOnlyDictionary<string, string[]> errors) =>
        new() { Success = false, Errors = errors };
}

/// <summary>Результат вызова API с полезной нагрузкой.</summary>
public class ApiResult<T> : ApiResult
{
    public T? Value { get; init; }

    public static ApiResult<T> Ok(T value) => new() { Success = true, Value = value };

    public new static ApiResult<T> Fail(IReadOnlyDictionary<string, string[]> errors) =>
        new() { Success = false, Errors = errors };
}
