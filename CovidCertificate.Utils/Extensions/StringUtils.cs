using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CovidCertificate.Backend.Utils.Extensions
{
    public static class StringUtils
    {
        public const string NumberFormattedEnumFormat = "D";

        public static readonly string UnknownCountryString = "UNKNOWN-COUNTRY";

        public static readonly string NhsNumberRegex = "^[0-9]{10}$";

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
        }

        public static string RandomDigitCode(int length)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(RandomNumberGenerator.GetInt32(10));
            }

            return sb.ToString();
        }

        public static string GetHashString(this string inputString)
        {
            var sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        private static byte[] GetHash(string inputString)
        {
            var inputByte = Encoding.Unicode.GetBytes(inputString);
            var hasher = SHA256.Create();
            return hasher.ComputeHash(inputByte);
        }

        public static string GetHashValue(string destination, string name, DateTime dob)
        {
            if (string.IsNullOrEmpty(destination))
                return "";
            if (string.IsNullOrEmpty(name))
                return "";
            if (dob == default)
                return "";

            return (name.ToLower() + destination.ToLower() + dob.ToString("dd-MM-yy")).GetHashString();
        }

        public static string GetHashValue(string nhsNumber, DateTime dob)
        {
            if (string.IsNullOrEmpty(nhsNumber))
                return "";
            if (dob == default)
                return "";

            //This needs to match the vaccination format for storing hash records for retrial
            return (nhsNumber.ToLower() + dob.ToString(DateUtils.DateFormat)).GetHashString();
        }

        public static Stream GetStream(this string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;

            return stream;
        }

        public static string MaxLength(this string s, int maxLength)
            => s?.Substring(0, s.Length <= maxLength ? s.Length : Math.Max(0, maxLength));

        public static string UtcTimeZoneConverterWithTranslationOption(DateTime dateTimeToConvert, TimeZoneInfo timeZoneInfo, string cultureInfo = "en")
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dateTimeToConvert, DateTimeKind.Unspecified), timeZoneInfo).ToString("d MMMM yyyy", new CultureInfo(cultureInfo));
        }

        public static string UseDashForEmptyOrBlankValues(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        public static string GetTranslatedAndFormattedDateTime(DateTime dateTime, string cultureInfo = "en")
        {
            var formattedString = dateTime.ToString("d MMMM yyyy 'at' h.mm", new CultureInfo(cultureInfo)) 
                + dateTime.ToString("tt").ToLower();

            return formattedString;
        }

        public static string GetTranslatedAndFormattedDate(DateTime dateTime, string cultureInfo = "en")
        {
            return dateTime.ToString("d MMMM yyyy", new CultureInfo(cultureInfo));
        }

        public static bool FlagEqualsFalse(this string input)
        {
            return input?.Equals(bool.FalseString, StringComparison.InvariantCultureIgnoreCase) == true;
        }

        public static bool FlagEqualsTrue(this string input)
        {
            return input?.Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase) == true;
        }
    }
}
