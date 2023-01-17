using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class ForbiddenException: Exception
    {
        public ForbiddenException(string message) : base(message)
        {
        }

        public ForbiddenException(string message,
            Exception innerException): base(message, innerException)
        {
        }
    }
}
