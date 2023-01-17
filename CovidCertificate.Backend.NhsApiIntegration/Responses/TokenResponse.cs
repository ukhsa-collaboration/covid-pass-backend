using Newtonsoft.Json;

namespace CovidCertificate.Backend.NhsApiIntegration.Responses
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("issued_at")]
        public long IssuedAt {get; set; }
    }
}
