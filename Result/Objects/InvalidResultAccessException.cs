using System;

namespace Result.Objects
{
    public class InvalidResultAccessException : Exception
    {
        public InvalidResultAccessException(string message) : base(message)
        {
        }
    }
}