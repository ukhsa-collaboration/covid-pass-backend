using System;
using System.IO;
using System.Text;

namespace CovidCertificate.Backend.Utils
{
    public static class DomesticExemptionUtils
    {
        public static string ReadExemptionFile(string request)
        {
            var stringBuilder = new StringBuilder();
            using var reader = new StringReader(request);

            for (string line = reader.ReadLine(); !string.IsNullOrEmpty(line); line = reader.ReadLine())
            {
                stringBuilder.AppendLine(line.Trim('"'));
            }

            return stringBuilder.ToString();
        }

        public static bool ValidateDoB(DateTime dateOfBirth, DateTime minDateOfBirth, DateTime maxDateOfBirth)
        {
            return dateOfBirth > minDateOfBirth && dateOfBirth < maxDateOfBirth;
        }
    }
}
