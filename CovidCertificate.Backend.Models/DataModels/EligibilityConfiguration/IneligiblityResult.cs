using System;

namespace CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration
{
    public class IneligiblityResult
    {
        public int? ErrorCode { get; set; }
        public DateTime? WaitPeriod { get; set; }

        public IneligiblityResult(int? errorCode = null, DateTime? waitPeriod = null)
        {
            ErrorCode = errorCode;
            WaitPeriod = waitPeriod;
        }
    }
}
