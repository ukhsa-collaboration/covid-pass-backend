using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace CovidCertificate.Backend.Auth.Models
{
    [OpenApiExample(typeof(RefreshTokenRequestModelExample))]
    public class RefreshTokenRequestModel
    {
        public string redirectUri;

        public RefreshTokenRequestModel(string redirectUri)
        {
            this.redirectUri = redirectUri;
        }
    }
}
