using System;

namespace CovidCertificate.Backend.Interfaces.DateTimeProvider
{
    public interface IDateTimeProviderService
    {
        public System.DateTime UtcNow { get; }
    }
}
