using CovidCertificate.Backend.Models.Enums;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration
{
    public class EligibilityResult
    {
        public ResultStatus Name { get; private set; }

        [JsonConstructor]
        public EligibilityResult(ResultStatus name)
        {
            Name = name;
        }
    }
}
