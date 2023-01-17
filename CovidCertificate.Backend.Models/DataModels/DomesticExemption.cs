using System;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class DomesticExemption
    {
        public string ExemptionReason { get; set; }
        public DateTime? DateExemptionExpires { get; set; }

        public DomesticExemption(string exemptionReason, DateTime? dateExemptionExpires)
        {
            ExemptionReason = exemptionReason;
            DateExemptionExpires = dateExemptionExpires;
        }

        public DomesticExemption(DomesticExemptionRecord domesticExemption)
        {
            ExemptionReason = domesticExemption.Reason;
        }
    }
}
