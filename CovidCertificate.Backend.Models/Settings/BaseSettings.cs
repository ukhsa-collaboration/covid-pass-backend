using Microsoft.Extensions.Configuration;

namespace CovidCertificate.Backend.Models.Settings
{
    public class BaseSettings
    {
        public string KeyVaultSecret { get; set; }

        public BaseSettings(IConfiguration configuration, string secretName)
        {
            KeyVaultSecret = configuration[secretName];
        }

        public BaseSettings() { }
    }


}