using System;
using System.Globalization;
using System.Linq;

namespace CovidCertificate.Backend.Utils
{
    public class DateUtils
    {
        public static readonly string DateFormat = "yyyyMMdd";
        public static readonly string FHIRDateFormat = "yyyy-MM-dd";
        public static readonly string DomesticExemptionDateFormat = "yyyy-MM-dd";
        public static readonly string HttpRequestDateFormat = "yyyy-MM-dd";
        public static readonly string EffectiveDateFormat = "eqyyyy-MM-ddThh:mm:ssK";
        public static readonly string LastChangeDateFormat = "yyyy-MM-dd";

        public static bool CheckIfDateFormatIsCorrect(string date, string dateFormat)
        {
            return DateTime.TryParseExact(date, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime);
        }

        public static bool CheckIfDateDormatIsCorrectAndNotInTheFuture(string date, string dateFormat)
        {
            if (DateTime.TryParseExact(date, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            {
                return DateTime.Now >= dateTime;
            }
            return false;
        }

        public static DateTime MinimumOfTwoDates(DateTime date1, DateTime date2)
        {
            return new[] {
                date1,
                date2
            }.Min();
        }

        public static DateTime UnixTimeSecondsToDateTime(long unixTimeSeconds)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeSeconds);

            return dateTime;
        }

        public static int GetAgeInYears(DateTime dateOfBirthInUTC)
        {
            var age = DateTime.UtcNow.Year - dateOfBirthInUTC.Year;

            if (dateOfBirthInUTC.Date > DateTime.Today.AddYears(-age))
                age--;

            return age;
        }

        public static bool AgeIsBelowLimit(DateTime dateOfBirthInUTC, int ageLimit)
        {
            var age = DateUtils.GetAgeInYears(dateOfBirthInUTC);

            if (age < ageLimit)
                return true;

            return false;
        }
    }
}
