using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace CovidCertificate.Backend.Models.Deserializers
{
    public static class FHIRDeserializer
    {
        private static FhirJsonParser fhirJsonParser = new FhirJsonParser(new ParserSettings
        {
            AcceptUnknownMembers = true,
            AllowUnrecognizedEnums = true
        });

        public static T Deserialize<T>(string fhirJson) where T: Base
        {
            var bundle = fhirJsonParser.Parse<T>(fhirJson);

            return bundle;
        }
    }
}
