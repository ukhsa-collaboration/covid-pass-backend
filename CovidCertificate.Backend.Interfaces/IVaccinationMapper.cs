using CovidCertificate.Backend.Models.DataModels;
using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IVaccinationMapper
    {
        Task<IEnumerable<Vaccine>> MapBundleToVaccinesAsync(Bundle bundle);

        Task<Vaccine> MapFhirToVaccineAsync(Immunization immunization, string issuer = null, string countryOverride = null);
        
        Task<Vaccine> MapFhirToVaccineAndAllowOverwriteOfSeriesDosesFromMappingFileAsync(Immunization immunization, string issuer = null, string countryOverride = null);

        DAUser DAUserFromPatient(Patient patient);

        Task<VaccineMap> MapRawSnomedcodeValueAsync(string rawSnomedcodeValue);
    }
}
