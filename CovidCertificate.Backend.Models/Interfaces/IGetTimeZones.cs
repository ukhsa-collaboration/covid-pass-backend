using System;

namespace CovidCertificate.Backend.Models.Interfaces
{
    public interface IGetTimeZones
    { 
        public TimeZoneInfo GetTimeZoneInfo();
    }
}
