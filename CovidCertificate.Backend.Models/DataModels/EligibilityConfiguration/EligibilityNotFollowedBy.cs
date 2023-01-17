using System.Collections.Generic;
using CovidCertificate.Backend.Models.Enums;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration
{
    public class EligibilityNotFollowedBy
    {
        public DataType ProductType { get; private set; }
        public string Name { get; private set; }
        public IEnumerable<EligibilityResult> Results { get; private set; }

        [JsonConstructor]
        public EligibilityNotFollowedBy(DataType resultType, string name, IEnumerable<EligibilityResult> results)
        {
            ProductType = resultType;
            Name = name;
            Results = results;
        }
    }
}
