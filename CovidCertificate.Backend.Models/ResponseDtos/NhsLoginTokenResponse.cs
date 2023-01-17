using System.Text;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Models.ResponseDtos
{
    public class NhsLoginTokenResponse
    {
        public NhsLoginTokenResponse(NhsLoginToken token) 
        {
            this.AccessToken = token.AccessToken;
            this.RefreshToken = token.RefreshToken;
            this.ExpiresIn = token.ExpiresIn;
            this.IdToken = token.IdToken;
        }

        public NhsLoginTokenResponse(string accessToken, string refreshToken, string expiresIn, string idToken)
        {
            this.AccessToken = accessToken;
            this.RefreshToken = refreshToken;
            this.ExpiresIn = expiresIn;
            this.IdToken = idToken;
        }

        public string AccessToken { get; }
        public string RefreshToken { get; }
        public string ExpiresIn { get; }
        public string IdToken { get; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("AccessToken:").Append(this.AccessToken??"").AppendLine();
            sb.Append("ExpiresIn:").Append(this.ExpiresIn??"").AppendLine();
            sb.Append("IdToken:").Append(this.IdToken??"").AppendLine();
            sb.Append("RefreshToken:").Append(this.RefreshToken??"").AppendLine();

            return sb.ToString();
        }
    }
}
