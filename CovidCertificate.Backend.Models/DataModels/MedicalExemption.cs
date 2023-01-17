using System;
using CovidCertificate.Backend.Models.Enums;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class MedicalExemption : DomesticExemption
    {
        public DateTime? DateExemptionGiven { get; set; }
        public ExemptionReasonCode? ExemptionReasonCode { get; set; }

        [JsonConstructor]
        public MedicalExemption(string exemptionReason, 
                                ExemptionReasonCode exemptionReasonCode,
                                DateTime? dateExemptionGiven,
                                DateTime? dateExemptionExpires) : base(exemptionReason, dateExemptionExpires)
        {
            ExemptionReasonCode = exemptionReasonCode;
            DateExemptionGiven = dateExemptionGiven;
        }

        public MedicalExemption(DomesticExemptionRecord exemption) : base(exemption) { }
    }
}
