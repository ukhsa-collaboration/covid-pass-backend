using System;
using System.Linq;
using CovidCertificate.Backend.Models.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CovidCertificate.Backend.Services
{
    public class GetTimeZones : IGetTimeZones
    {
        private readonly IConfiguration configuration;
        public GetTimeZones(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public TimeZoneInfo GetTimeZoneInfo()
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones().Select(x => x.Id);
            TimeZoneInfo timeZoneInfo = TimeZoneInfo.Utc;
            if (timeZones.Contains(configuration["TimeZoneWindows"]))
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(configuration["TimeZoneWindows"]);
            if (timeZones.Contains(configuration["TimeZoneLinux"]))
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(configuration["TimeZoneLinux"]);
            return timeZoneInfo;
        }
    }
}
