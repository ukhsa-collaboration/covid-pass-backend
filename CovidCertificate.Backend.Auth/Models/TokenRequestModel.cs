using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace CovidCertificate.Backend.Auth.Models
{
    [OpenApiExample(typeof(TokenRequestModelExample))]
    public class TokenRequestModel
    {
        public string code;
        public string redirectUri;

        public TokenRequestModel(string code, string redirectUri)
        {
            this.code = code;
            this.redirectUri = redirectUri;
        }
    }
}

