namespace CovidCertificate.Backend.UnattendedCertificate.ErrorHandling
{
    public enum ErrorCode : ushort
    {
        UNKNOWN_ERROR = 1,
        DISABLED_ENDPOINT = 5,
        FHIR_PATIENT_INVALID = 10,
        FHIR_PATIENT_NHS_NUMBER_MISSING = 11,
        FHIR_PATIENT_NHS_NUMBER_INVALID = 12,
        FHIR_PATIENT_NAME_MISSING = 13,
        FHIR_PATIENT_BIRTHDATE_MISSING = 14,
        FHIR_PATIENT_UNDERAGE = 15,
        POSITIVE_PCR_FOUND = 102,
        POSITIVE_LFT_FOUND = 103,
        NO_VACCINES_FOUND = 201,
        INVALID_VACCINES = 202,
        NO_TEST_RESULTS_GRANTING_RECOVERY_FOUND = 206,
        ILLEGAL_VALUE_PASSED = 210,
        NO_RECORDS_FOUND = 401,
        EVENT_HISTORY_NOT_MET = 402,
        
    }
}
