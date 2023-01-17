using Microsoft.Extensions.Configuration;
using System;

namespace CovidCertificate.Backend.Models.Settings
{
    public class EmailSenderCredentialSettings : BaseSettings
    {
        private string sendGridApiKey;

        public string SendGridApiKey
        {
            get
            {
                if (string.IsNullOrEmpty(sendGridApiKey))
                {
                    throw new ArgumentNullException(nameof(SendGridApiKey), "API Key must be set first");
                }
                return sendGridApiKey;
            }
            set { sendGridApiKey = value; }
        
        }

        private string fromEmail;

        public string FromEmail
        {
            get
            {
                if (string.IsNullOrEmpty(fromEmail))
                {
                    throw new ArgumentNullException(nameof(fromEmail), "From email address must be set first");
                }

                return fromEmail;
            }
            set { fromEmail = value; }
        }

        public string FromEmailName { get; set; }
        public string EmailViewsFolder { get; set; }

        public EmailSenderCredentialSettings(IConfiguration configuration, string secretName) : base(configuration, secretName)
        {

        }
    }
}
