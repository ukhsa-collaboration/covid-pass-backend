using System;

namespace CovidCertificate.Backend.Models.PKINationalBackend
{
    public class EUValueSetResponse
    {
        public EUValueSet ValueSets { get; set; }
        public DateTime ValueSetDate { get; set; }

        public EUValueSetResponse(EUValueSet valueSets, DateTime valueSetDate)
        {
            ValueSets = valueSets;
            ValueSetDate = valueSetDate;
        }
    }
}
