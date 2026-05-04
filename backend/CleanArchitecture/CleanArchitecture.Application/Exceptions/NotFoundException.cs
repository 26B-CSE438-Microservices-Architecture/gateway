using System;

namespace CleanArchitecture.Core.Exceptions
{
    public class NotFoundException : ApiException
    {
        public NotFoundException(string errorCode, string message) : base(errorCode, message)
        {
        }
    }
}
