using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class UnauthorizedUnattendedApiCallException : Exception
    {
        public UnauthorizedUnattendedApiCallException(string message) : base(message)
        {
        }
    }
}
