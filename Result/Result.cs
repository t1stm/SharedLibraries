#nullable enable
using System;
using System.Collections.Generic;
using Result.Objects;

namespace Result
{
    public class Result<T_OK, T_Error>
    {
        protected readonly T_Error? ErrorValue;
        protected readonly T_OK? OkValue;
        protected readonly Status Status;

        public Result(T_OK? okValue, T_Error? errorValue, Status status)
        {
            OkValue = okValue;
            ErrorValue = errorValue;
            Status = status;
        }

        public T_OK GetOK()
        {
            return this == Status.OK
                ? OkValue ?? throw new NullReferenceException("OK value is null.")
                : throw new InvalidResultAccessException("Tried to get OK result when status is \'Error\'.");
        }

        public T_Error GetError()
        {
            return this == Status.Error
                ? ErrorValue ?? throw new NullReferenceException("Error value is null.")
                : throw new InvalidResultAccessException("Tried to get Error result when status is \'OK\'.");
        }

        public static Result<T_OK, T_Error> Success(T_OK ok)
        {
            return new(ok, default, Status.OK);
        }

        public static Result<T_OK, T_Error> Error(T_Error error)
        {
            return new(default, error, Status.Error);
        }

        public static bool operator ==(Result<T_OK, T_Error> source, Status status)
        {
            return source.Status == status;
        }

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
}