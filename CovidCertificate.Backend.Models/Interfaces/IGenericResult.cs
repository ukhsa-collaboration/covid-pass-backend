using System;

namespace CovidCertificate.Backend.Models.Interfaces
{
    //Interface for diagnostic and vaccination models to inherit so that they can be worked
    // on as a whole in the eligibility configuration business rules
    public interface IGenericResult
    {
        public DateTime DateTimeOfTest { get; }
        public string ValidityType { get; }
        public string Result { get; }
        public string CountryCode { get; }
    }
}
