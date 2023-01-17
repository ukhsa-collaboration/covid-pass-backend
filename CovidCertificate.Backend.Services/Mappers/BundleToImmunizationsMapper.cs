using System.Collections.Generic;
using CovidCertificate.Backend.Interfaces;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.Mappers
{
    public class BundleToImmunizationsMapper : IMapper<Bundle, List<Immunization>>
    {
        private ILogger<BundleToImmunizationsMapper> logger;

        public BundleToImmunizationsMapper(ILogger<BundleToImmunizationsMapper> logger)
        {
            this.logger = logger;
        }

        public List<Immunization> MapAsync(Bundle bundle)
        {
            var mappedImmunizations = new List<Immunization>();

            foreach (var entry in bundle.Entry)
            {
                if (entry?.Resource is Immunization immunization)
                {
                    mappedImmunizations.Add(immunization);
                }
            }
            
            return mappedImmunizations;
        }
    }
}
