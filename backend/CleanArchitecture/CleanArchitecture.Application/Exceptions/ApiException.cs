using System;
using System.Globalization;

namespace CleanArchitecture.Core.Exceptions
{
    public class ApiException : Exception
    {
        public string ErrorCode { get; }

        public ApiException() : base() { }

        public ApiException(string message) : base(message) { }

        public ApiException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public ApiException(string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args))
        {
        }
    }
}
