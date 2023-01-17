using System.Collections.Generic;
using CovidCertificate.Backend.Models.DataModels;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.NhsApiIntegration.Interfaces
{
    public interface IMedicalExemptionDataParser
    {
        IEnumerable<MedicalExemption> Parse(Bundle bundle);
    }
}
