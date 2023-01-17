using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace CovidCertificate.Backend.Models
{
    public static class FHIRSerializer
    {
        private static FhirJsonSerializer fhirJsonSerializer = new FhirJsonSerializer();

        public static string Serialize(Base baseObject)
        {
            return fhirJsonSerializer.SerializeToString(baseObject);
        }
    }
}
