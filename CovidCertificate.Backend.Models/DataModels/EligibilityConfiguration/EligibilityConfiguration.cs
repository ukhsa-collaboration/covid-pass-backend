using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration
{
    public class EligibilityConfiguration
    {
        public IEnumerable<EligibilityRules> Rules { get; set; }
        public EligibilityDomesticExemptions DomesticExemptions { get; set; }
        public Dictionary<string, IEnumerable<string>> AllowedCountries { get; set; }
        public IneligibilityPeriod Ineligibility { get; set; }
        [JsonConstructor]
        public EligibilityConfiguration() { }

        public EligibilityConfiguration(IEnumerable<EligibilityRules> rules, EligibilityDomesticExemptions domesticExemptions, IneligibilityPeriod ineligibility)
        {
            Rules = rules;
            DomesticExemptions = domesticExemptions;
            Ineligibility = ineligibility;
        }
    }
}
