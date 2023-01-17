using System.Collections.Generic;

namespace CovidCertificate.Backend.PKINationalBackend.Models
{
    public class DGCGValueSetAPIResponse
    {
        public Dictionary<string, Dictionary<string, string>> valueSetValues { get; set; }
    }
}
