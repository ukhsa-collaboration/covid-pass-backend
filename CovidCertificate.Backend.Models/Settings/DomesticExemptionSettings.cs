using System.ComponentModel.DataAnnotations;

namespace CovidCertificate.Backend.Models.Settings
{
    public class DomesticExemptionSettings
    {
        public int InMemoryTimeToLiveSeconds { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string SaveQueueName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string RemoveQueueName { get; set; }
    }
}
