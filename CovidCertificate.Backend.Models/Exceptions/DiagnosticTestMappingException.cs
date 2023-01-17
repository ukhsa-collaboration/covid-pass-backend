using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class DiagnosticTestMappingException : Exception
    {
        public DiagnosticTestMappingException(string message) : base(message)
        {
        }
    }
}
