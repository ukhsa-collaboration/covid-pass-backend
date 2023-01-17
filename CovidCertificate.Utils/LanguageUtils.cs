using System.Globalization;

namespace CovidCertificate.Backend.Utils
{
    public class LanguageUtils
    {
        public static bool ValidCountryCode(string country)
        {
            if (country.Length == 2)
            {
                CultureInfo[] all = CultureInfo.GetCultures(CultureTypes.AllCultures);

                foreach (CultureInfo culture in all)
                {
                    if (culture.TwoLetterISOLanguageName.Equals(country))
                        return true;
                }
            }
            return false;
        }
    }
}