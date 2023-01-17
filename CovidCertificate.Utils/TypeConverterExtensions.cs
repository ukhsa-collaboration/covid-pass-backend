using System;

namespace CovidCertificate.Backend.Utils
{
    public static class TypeConverterExtensions
    {
        public static byte[] FromBase64StringToByteArray(this string value) => Convert.FromBase64String(value);
    }
}