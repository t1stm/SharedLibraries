#nullable enable
using Result.Objects;

namespace Result;

public class Result<T_OK, T_Error>
{
    protected readonly T_Error? ErrorValue;
    protected readonly T_OK? OkValue;
    protected readonly Status Status;
    
    /// <summary>
    /// Creates a result object.
    /// </summary>
    /// <param name="okValue">Value when successfully returned.</param>
    /// <param name="errorValue">Value returned when failed.</param>
    /// <param name="status">The status code of the result.</param>
    protected Result(T_OK? okValue, T_Error? errorValue, Status status)
    {
        OkValue = okValue;
        ErrorValue = errorValue;
        Status = status;
    }

    /// <summary>
    /// Gets the OK object when the status is OK.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NullReferenceException">Thrown when the OK object is null. This ensures null safety.</exception>
    /// <exception cref="InvalidResultAccessException">Thrown when the status isn't Status.OK</exception>
    public T_OK GetOK()
    {
        return this == Status.OK
            ? OkValue ?? throw new NullReferenceException("OK value is null.")
            : throw new InvalidResultAccessException("Tried to get OK result when status is \'Error\'.");
    }
    
    /// <summary>
    /// Gets the Error object when the status is Error.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NullReferenceException">Thrown when the Error object is null. This ensures null safety.</exception>
    /// <exception cref="InvalidResultAccessException">Thrown when the status isn't Status.Error</exception>
    public T_Error GetError()
    {
        return this == Status.Error
            ? ErrorValue ?? throw new NullReferenceException("Error value is null.")
            : throw new InvalidResultAccessException("Tried to get Error result when status is \'OK\'.");
    }

    /// <summary>
    /// Creates a new Result object with the status: Status.OK
    /// </summary>
    /// <param name="ok">The object to assign to T_OK</param>
    /// <returns>The created Result object.</returns>
    public static Result<T_OK, T_Error> Success(T_OK ok)
    {
        return new Result<T_OK, T_Error>(ok, default, Status.OK);
    }

    /// <summary>
    /// Creates a new Result object with the status: Status.Error
    /// </summary>
    /// <param name="error">The object to assign to T_Error</param>
    /// <returns>The created Result object.</returns>
    public static Result<T_OK, T_Error> Error(T_Error error)
    {
        return new Result<T_OK, T_Error>(default, error, Status.Error);
    }

    /// <summary>
    /// Compare the current result with a status code.
    /// </summary>
    /// <param name="source">The source Result object.</param>
    /// <param name="status">The status code to compare.</param>
    /// <returns>Whether the Result object has a matching status.</returns>
    public static bool operator ==(Result<T_OK, T_Error> source, Status status)
    {
        return source.Status == status;
    }
    
    /// <summary>
    /// Compare the current result with a status code.
    /// </summary>
    /// <param name="source">The source Result object.</param>
    /// <param name="status">The status code to compare.</param>
    /// <returns>Whether the Result object doesn't have a matching status.</returns>
    public static bool operator !=(Result<T_OK, T_Error> source, Status status)
    {
        return !(source == status);
    }
    
    protected bool Equals(Result<T_OK, T_Error> other)
    {
        return EqualityComparer<T_OK>.Default.Equals(OkValue, other.OkValue) &&
               EqualityComparer<T_Error>.Default.Equals(ErrorValue, other.ErrorValue) &&
               Status.Equals(other.Status);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() &&
               Equals((Result<T_OK, T_Error>) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(OkValue, ErrorValue, Status);
    }
}