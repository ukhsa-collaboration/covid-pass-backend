using System.Collections.Generic;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.PKINationalBackend
{
    public class DGCGTrustList
    {
        public IEnumerable<DocumentSignerCertificate> Certificates;

        [JsonConstructor]
        public DGCGTrustList(IEnumerable<DocumentSignerCertificate> certificates)
        {
            Certificates = certificates;
        }
    }
}
