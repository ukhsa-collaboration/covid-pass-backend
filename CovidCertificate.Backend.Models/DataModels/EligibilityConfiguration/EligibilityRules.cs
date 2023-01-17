using System.Collections.Generic;
using CovidCertificate.Backend.Models.Enums;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration
{
    public class EligibilityRules
    {
        public string ConfigurationName { get; private set; }
        public CertificateType CertificateType { get; private set; }
        public CertificateScenario Scenario { get; private set; }
        public IEnumerable<EligibilityCondition> Conditions { get; set; }
        public int ValidityPeriodHours { get; private set; }
        public TimeFormat FormatExpiry { get; private set; }
        public int? PolicyMask { get; set; }
        public string[] Policy { get; set; }

        [JsonConstructor]
        public EligibilityRules(
            string configurationName, 
            CertificateType certificateType, 
            CertificateScenario scenario, 
            IEnumerable<EligibilityCondition> conditions, 
            int validityPeriodHours, TimeFormat formatExpiry, 
            int? policyMask = null, 
            string[] policy = null)
        {
            ConfigurationName = configurationName;
            CertificateType = certificateType;
            Scenario = scenario;
            Conditions = conditions;
            ValidityPeriodHours = validityPeriodHours;
            FormatExpiry = formatExpiry;
            PolicyMask = policyMask;
            Policy = policy;
        }

        public static EligibilityRules Copy(EligibilityRules eligibilityRules)
        {

            var conditionsCopy = new List<EligibilityCondition>();
            foreach (var condition in eligibilityRules.Conditions)
            {
                var conditionCopy = EligibilityCondition.Copy(condition);
                conditionCopy.ResultValidAfterHoursFromLastResult = 0;
                conditionsCopy.Add(conditionCopy);
            }

            return new EligibilityRules(
                eligibilityRules.ConfigurationName, 
                eligibilityRules.CertificateType, 
                eligibilityRules.Scenario,
                conditionsCopy,
                eligibilityRules.ValidityPeriodHours,
                eligibilityRules.FormatExpiry,
                eligibilityRules.PolicyMask,
                eligibilityRules.Policy
            );
        }
    }
}
