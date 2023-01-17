using System;
using System.Collections.Generic;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration
{
    public class CertificateMetadata
    {
        public DateTime Eligibility { get; set; }
        public DateTime Expiry { get; set; }

        public IEnumerable<IGenericResult> EligibilityResults { get; private set; }

        public CertificateMetadata(DateTime expiry, DateTime eligibiity, IEnumerable<IGenericResult> eligibilityResults)
        {
            Eligibility = eligibiity;
            Expiry = expiry;
            EligibilityResults = eligibilityResults;
        }        
    }
}
