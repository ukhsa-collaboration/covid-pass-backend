using System;
using System.Collections.Generic;

namespace CovidCertificate.Backend.Models.PKINationalBackend.DomesticPolicy
{
    public class DomesticPolicyInformation
    {
        public AcceptedPolicies AcceptedPolicies { get; set; }
        public IEnumerable<string> EnglishCertificateIssuers { get; set; }
        public Dictionary<string, int> InternationalMinimumDoses { get; set; }
        public int TestResultValidForHours { get; set; }
        public DateTime LastUpdated { get; set; }

        public DomesticPolicyInformation() { }
    }
}
