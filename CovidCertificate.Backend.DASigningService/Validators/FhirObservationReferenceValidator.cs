using System.Collections.Generic;
using System.Linq;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.DASigningService.Validators
{
    public class FhirObservationReferenceValidator : AbstractValidator<Bundle>
    {
        public FhirObservationReferenceValidator()
        {
            RuleFor(x => x.Entry).Cascade(CascadeMode.Stop).Must(HasValidObservationPerformerReferences)
            .WithMessage("Observation.Performer[0].Reference invalid.")
            .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_PERFORMER_REFERENCE_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Entry).Cascade(CascadeMode.Stop).Must(HasValidObservationDeviceReferences)
            .WithMessage("Observation.Device.Reference invalid.")
            .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_DEVICE_REFERENCE_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));
        }

        private bool HasValidObservationPerformerReferences(List<Bundle.EntryComponent> entries)
        {
            List<Observation> observations = GetObservations(entries);

            foreach (var observation in observations)
            {
                if (observation.Performer != null && observation.Performer.Count > 0 && observation.Performer[0].Reference != null)
                {
                    IEnumerable<Bundle.EntryComponent> matchingOrganizations =
                        entries.Where(x => x.Resource is Organization && x.FullUrl != null && x.FullUrl.Equals(observation.Performer[0].Reference));

                    return matchingOrganizations.Count() == 1;
                }
            }
            return true;
        }

        private bool HasValidObservationDeviceReferences(List<Bundle.EntryComponent> entries)
        {
            List<Observation> observations = GetObservations(entries);

            foreach (var observation in observations)
            {
                if (observation.Device != null && observation.Device.Reference != null)
                {
                    IEnumerable<Bundle.EntryComponent> matchingDevices =
                        entries.Where(x => x.Resource is Device && x.FullUrl != null && x.FullUrl.Equals(observation.Device.Reference));

                    return matchingDevices.Count() == 1;
                }
            }
            return true;
        }

        private List<Observation> GetObservations(List<Bundle.EntryComponent> entries)
        {
            List<Observation> observations = new List<Observation>();
            foreach (var entry in entries)
            {
                if (entry?.Resource is Observation o)
                {
                    observations.Add(o);
                }
            }
            return observations;
        }
    }
}
