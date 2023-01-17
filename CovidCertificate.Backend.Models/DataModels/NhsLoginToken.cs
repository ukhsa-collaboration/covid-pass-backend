using System.Text;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class NhsLoginToken
    {
        [JsonRequired, JsonProperty("access_token")]
        public string AccessToken { get; private set; }
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; private set; }
        [JsonProperty("id_token")]
        public string IdToken { get; private set; }
        [JsonRequired, JsonProperty("expires_in")]
        public string ExpiresIn { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("AccessToken:").Append(this.AccessToken??"").AppendLine();
            sb.Append("ExpiresIn:").Append(this.ExpiresIn??"").AppendLine();
            sb.Append("IdToken").Append(this.IdToken??"").AppendLine();
            sb.Append("RefreshToken:").Append(this.RefreshToken??"").AppendLine();

            return sb.ToString();
        }
    }
}
