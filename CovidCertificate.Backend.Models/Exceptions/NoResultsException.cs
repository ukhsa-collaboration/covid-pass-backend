using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class NoResultsException: Exception
    {
        public NoResultsException(string message) : base(message) { }
    }
}