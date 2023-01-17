using System;
using CovidCertificate.Backend.Utils.Extensions;

namespace CovidCertificate.Backend.Utils
{
    public static class HashUtils
    {
        /// <summary>
        /// This method calculate hash from <c>'healtId+dob'</c> string. <c>'Dob'</c> should be in <c>'yyyyMMdd'</c> format.
        /// </summary>
        /// <param name="healthId"></param>
        /// <param name="dob">Should be in format <c>yyyyMMdd</c></param>
        /// <returns></returns>
        public static string GenerateHash(string healthId, string dob)
        {
            var totalString = healthId + dob;
            return totalString.GetHashString();
        }

        public static string GenerateHash(string healthId, DateTime dob)
        {
            var dateString = dob.ToLocalTime().ToString(DateUtils.DateFormat);
            return GenerateHash(healthId, dateString);
        }
    }
}
