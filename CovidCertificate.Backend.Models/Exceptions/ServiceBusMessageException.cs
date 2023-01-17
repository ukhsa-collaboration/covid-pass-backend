using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class ServiceBusMessageException : Exception
    {
        public ServiceBusMessageException(string message) : base(message)
        {
        }
    }
}
