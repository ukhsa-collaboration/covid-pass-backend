using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class VaccineMappingException : Exception
    {
        public VaccineMappingException(string message) : base(message)
        {
        }
    }
}