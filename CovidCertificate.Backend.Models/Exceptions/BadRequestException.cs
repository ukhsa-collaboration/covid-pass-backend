using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message)
        {
        }

        public BadRequestException(string message,
            Exception innerException) : base(message, innerException)
        {
        }
    }
}
