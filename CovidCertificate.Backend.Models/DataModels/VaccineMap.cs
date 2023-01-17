using System.Collections.Generic;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class VaccineMap
    {
        public List<string> Manufacturer { get; set; }
        public List<string> Disease { get; set; }
        public List<string> Product { get; set; }
        public List<string> Vaccine { get; set; }
        public int TotalSeriesOfDoses { get; set; }
        public string EligibilityRuleName { get; set; }

    }
}
