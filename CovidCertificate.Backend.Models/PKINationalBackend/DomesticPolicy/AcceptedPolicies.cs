using System.Collections.Generic;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.PKINationalBackend.DomesticPolicy
{
    public class AcceptedPolicies
    {
        [JsonProperty("GB-ENG")]
        public IEnumerable<Dictionary<string, object>> EnglishPolicies { get; private set; }
        [JsonProperty("GB-SCT")]
        public IEnumerable<Dictionary<string, object>> ScottishPolicies { get; private set; }
        [JsonProperty("GB-NIR")]
        public IEnumerable<Dictionary<string, object>> NorthernIrishPolicies { get; private set; }
        [JsonProperty("GB-WLS")]
        public IEnumerable<Dictionary<string, object>> WelshPolicies { get; private set; }
        [JsonProperty("JE")]
        public IEnumerable<Dictionary<string, object>> JerseyPolicies { get; private set; }
        [JsonProperty("GG")]
        public IEnumerable<Dictionary<string, object>> GuernseyPolicies { get; private set; }

        public AcceptedPolicies() { }
    }
}
