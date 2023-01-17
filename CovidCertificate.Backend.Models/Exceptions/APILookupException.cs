using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class APILookupException: Exception
    {
        public APILookupException(string message) : base(message)
        {
        }
    }
}
