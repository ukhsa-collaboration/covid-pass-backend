using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CovidCertificate.Backend.DASigningService.ErrorHandling
{
    public class ErrorHandler
    {
        private static readonly Dictionary<ErrorCode, string> ErrorMessages = new Dictionary<ErrorCode, string>
        {
            {ErrorCode.UNEXPECTED_SYSTEM_ERROR, "Unexpected system error." },
            {ErrorCode.CLIENT_CERTIFICATE_MISSING, "Client certificate missing."},
            {ErrorCode.INVALID_CLIENT_CERTIFICATE, "Invalid client certificate." },
            {ErrorCode.FHIR_INVALID, ""},
            {ErrorCode.UNSUPPORTED_TYPE, "Unsupported type." },
            {ErrorCode.ISSUER_MISSING, "Issuer missing." },
            {ErrorCode.FHIR_PATIENT_MISSING, "Patient missing." },
            {ErrorCode.FHIR_PATIENT_NAME_MISSING, "Patient name missing." },
            {ErrorCode.FHIR_PATIENT_GIVEN_NAME_MISSING, "Patient.Name[0].Given missing." },
            {ErrorCode.FHIR_PATIENT_FAMILY_NAME_MISSING, "Patient.Name[0].Family missing." },
            {ErrorCode.FHIR_PATIENT_BIRTHDATE_MISSING,"Patient.BirthDate missing." },
            {ErrorCode.FHIR_IMMUNIZATION_MISSING, "Immunization missing." },
            {ErrorCode.FHIR_IMMUNIZATION_VACCINECODE_MISSING, "Immunization.VaccineCode.Coding[0] missing." },
            {ErrorCode.FHIR_IMMUNIZATION_VACCINECODE_CODE_MISSING, "Immunization.VaccineCode.Coding[0].Code missing." },
            {ErrorCode.FHIR_IMMUNIZATION_OCCURENCEDATETIME_MISSING, "Immunization.OccurenceDateTime missing." },
            {ErrorCode.FHIR_IMMUNIZATION_LOTNUMBER_MISSING, "Immunization.LotNumber missing." },
            {ErrorCode.FHIR_IMMUNIZATION_PROTOCOLAPPLIED_DOSENUMBER_MISSING, "Immunization.ProtocolApplied[0].DoseNumber missing" },
        };

        public List<Error> Errors { get; }

        public ErrorHandler()
        {
            Errors = new List<Error>();
        }

        public void AddError(ErrorCode code)
        {
            Error error = new Error
            {
                Code = ((ushort) code).ToString(), Message = ErrorMessages.GetValueOrDefault(code, "")
            };
            Errors.Add(error);
        }

        public void AddError(ErrorCode code, string errorText)
        {
            Error error = new Error
            {
                Code = ((ushort) code).ToString(), Message = errorText
            };
            Errors.Add(error);
        }

        public bool HasErrors()
        {
            return Errors.Any();
        }

        public string GetErrorsAsJson()
        {
            return JsonSerializer.Serialize(Errors);
        }
    }
}
