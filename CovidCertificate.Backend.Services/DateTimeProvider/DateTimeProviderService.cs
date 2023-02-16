using System;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;

namespace CovidCertificate.Backend.Services.DateTimeProvider
{
    public class DateTimeProviderService: IDateTimeProviderService
    {
        public System.DateTime UtcNow => DateTime.UtcNow;
    }
}
