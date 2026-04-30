using System;

namespace azurefunction318.Models
{
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
