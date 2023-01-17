namespace CovidCertificate.Backend.Models.PKINationalBackend
{
    public class TrustListSubjectPublicKeyInfoDto
    {
        public string Kid { set; get; }
        public string PublicKey { set; get; }
        public string Country { set; get; }

        public TrustListSubjectPublicKeyInfoDto(string kid, string publicKey, string country)
        {
            Kid = kid;
            PublicKey = publicKey;
            Country = country;
        }
    }
}
