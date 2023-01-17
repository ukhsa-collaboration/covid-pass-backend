using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class TestResultApiException : Exception
    {
        public TestResultApiException(string message) : base(message)
        {
        }
    }
}
