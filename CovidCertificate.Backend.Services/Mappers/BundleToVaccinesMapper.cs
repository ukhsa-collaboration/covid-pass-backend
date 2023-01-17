using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.Mappers
{
    public class BundleToVaccinesMapper : IMapper<Bundle, Task<List<Vaccine>>>
    {
        private ILogger<BundleToImmunizationsMapper> logger;
        private IMapper<Bundle, List<Immunization>> bundleToImmunizationsMapper;
        private VaccinationMapper immunizationToVaccinationMapper;

        public BundleToVaccinesMapper(ILogger<BundleToImmunizationsMapper> logger, IMapper<Bundle, List<Immunization>> bundleToImmunizationsMapper,
            VaccinationMapper immunizationToVaccinationMapper)
        {
            this.logger = logger;
            this.bundleToImmunizationsMapper = bundleToImmunizationsMapper;
            this.immunizationToVaccinationMapper = immunizationToVaccinationMapper;
        }

        public async Task<List<Vaccine>> MapAsync(Bundle bundle)
        {
            var immunizations = bundleToImmunizationsMapper.MapAsync(bundle);
            List<Vaccine> vaccines = new List<Vaccine>();

            foreach (var immunization in immunizations)
            {
                vaccines.Add(await immunizationToVaccinationMapper.MapFhirToVaccineAsync(immunization));
            }

            return vaccines;
        }
    }
}

