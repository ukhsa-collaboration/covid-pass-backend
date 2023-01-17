using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class VaccinationApiException : Exception
    {
        public VaccinationApiException(string message) : base(message)
        {
        }
    }
}
