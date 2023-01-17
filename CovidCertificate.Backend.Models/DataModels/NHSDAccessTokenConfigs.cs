namespace CovidCertificate.Backend.Models.DataModels
{
    public class NHSDAccessTokenConfigs
    {
        public string PrivateKey { get; private set; }
        public string AppKid { get; private set; }
        public string AppKey { get; private set; }

        public NHSDAccessTokenConfigs(string privateKey, string appKid, string appKey)
        {
            PrivateKey = privateKey;
            AppKid = appKid;
            AppKey = appKey;
        }
    }
}
