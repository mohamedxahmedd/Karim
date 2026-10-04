namespace NationalAirspace.Application.Common;

/// <summary>
/// A validation or business error. <see cref="MessageKey"/> is a localization resource key;
/// <see cref="Field"/> is the input property the error belongs to (null for form-level errors).
/// </summary>
public sealed record Error(string MessageKey, string? Field = null, params object[] Args)
{
    public static Error NotFound { get; } = new("Error.NotFound");
    public static Error Concurrency { get; } = new("Error.Concurrency");
}

public class Result
{
    protected Result(IReadOnlyList<Error> errors) => Errors = errors;

    public IReadOnlyList<Error> Errors { get; }
    public bool Succeeded => Errors.Count == 0;
    public bool IsNotFound => Errors.Count == 1 && ReferenceEquals(Errors[0], Error.NotFound);

    public static Result Success() => new(Array.Empty<Error>());
    public static Result Failure(params Error[] errors) => new(errors);
    public static Result Failure(IEnumerable<Error> errors) => new(errors.ToArray());
    public static Result<T> Success<T>(T value) => new(value, Array.Empty<Error>());
}

public sealed class Result<T> : Result
{
    internal Result(T? value, IReadOnlyList<Error> errors) : base(errors) => Value = value;

    public T? Value { get; }

    public static implicit operator Result<T>(T value) => new(value, Array.Empty<Error>());

    public static new Result<T> Failure(params Error[] errors) => new(default, errors);
    public static new Result<T> Failure(IEnumerable<Error> errors) => new(default, errors.ToArray());
}
