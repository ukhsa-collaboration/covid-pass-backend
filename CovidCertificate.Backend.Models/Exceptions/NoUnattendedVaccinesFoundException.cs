using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class NoUnattendedVaccinesFoundException : Exception
    {
        public NoUnattendedVaccinesFoundException(string message) : base(message)
        {
        }
    }
}
