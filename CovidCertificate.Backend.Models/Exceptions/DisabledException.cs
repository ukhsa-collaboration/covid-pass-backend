using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class DisabledException : Exception
    {
        public DisabledException(string message) : base(message)
        {
        }
    }
}
