using System;

namespace CovidCertificate.Backend.Models.Helpers
{
    public class TimeFormatConvert
    {
        public static DateTime ToUniversal(DateTime value)
        {
            
            if (value == DateTime.MinValue || value == DateTime.MaxValue) return value;
           
            return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, DateTimeKind.Utc);
        }
    }
}
