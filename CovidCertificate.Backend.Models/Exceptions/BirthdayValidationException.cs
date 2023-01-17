using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class BirthdayValidationException : Exception
    {
        public BirthdayValidationException(string message) : base(message)
        {
        }
    }
}
