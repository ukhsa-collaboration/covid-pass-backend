﻿namespace CovidCertificate.Backend.DASigningService.ErrorHandling
{
    public enum ErrorCode : ushort
    {
        UNEXPECTED_SYSTEM_ERROR = 0,
        INVALID_CLIENT_CERTIFICATE = 1,
        FHIR_INVALID = 2,
        UNSUPPORTED_TYPE = 3,
        ISSUER_MISSING = 4,
        ENDPOINT_DISABLED = 5,
        CLIENT_CERTIFICATE_MISSING = 6,
        VALIDFROM_INVALID = 11,
        VALIDTO_INVALID = 21,
        POLICYMASK_MISSING = 50,
        POLICYMASK_INVALID = 51,
        POLICY_MISSING = 52,
        POLICY_INVALID = 53,
        FHIR_PATIENT_MISSING = 100,
        FHIR_PATIENT_NAME_MISSING = 101,
        FHIR_PATIENT_GIVEN_NAME_MISSING = 102,
        FHIR_PATIENT_FAMILY_NAME_MISSING = 103,
        FHIR_PATIENT_BIRTHDATE_MISSING = 104,
        FHIR_IMMUNIZATION_MISSING = 200,
        FHIR_IMMUNIZATION_VACCINECODE_MISSING = 201,
        FHIR_IMMUNIZATION_VACCINECODE_CODE_MISSING = 202,
        FHIR_IMMUNIZATION_OCCURENCEDATETIME_MISSING = 203,
        FHIR_IMMUNIZATION_LOTNUMBER_MISSING = 204,
        FHIR_IMMUNIZATION_PROTOCOLAPPLIED_DOSENUMBER_MISSING = 205,
        FHIR_IMMUNIZATION_PROTOCOLAPPLIED_DOSENUMBER_LARGER_THAN_SERIESDOSES = 206,
        FHIR_IMMUNIZATION_NOTBOOSTER_PROTOCOLAPPLIED_SERIESDOSES_LARGER_THAN_VACCINETYPE_SERIESDOSES = 207,
        FHIR_IMMUNIZATION_VACCINECODE_CODE_NOT_RECOGNIZED_AS_VALID_SNOMED = 208,
        FHIR_OBSERVATION_MISSING = 210,
        FHIR_OBSERVATION_VALUE_MISSING = 211,
        FHIR_OBSERVATION_VALUE_CODE_MISSING = 212,
        FHIR_OBSERVATION_VALUE_CODE_INVALID = 213,
        FHIR_OBSERVATION_EFFECTIVEDATETIME_MISSING = 214,
        FHIR_OBSERVATION_EFFECTIVEDATETIME_INVALID = 215,
        FHIR_OBSERVATION_DEVICE_MISSING = 216,
        FHIR_OBSERVATION_DEVICE_IDENTIFIER_MISSING = 217,
        FHIR_OBSERVATION_DEVICE_IDENTIFIER_VALUE_MISSING = 218,
        FHIR_OBSERVATION_DEVICE_IDENTIFIER_VALUE_INVALID = 219,
        FHIR_OBSERVATION_STATUS_MISSING = 220,
        FHIR_OBSERVATION_STATUS_INVALID = 221,
        FHIR_OBSERVATION_DEVICE_REFERENCE_MISSING = 222,
        FHIR_OBSERVATION_DEVICE_REFERENCE_INVALID = 223,
        FHIR_OBSERVATION_PERFORMER_MISSING = 224,
        FHIR_OBSERVATION_PERFORMER_REFERENCE_MISSING = 225,
        FHIR_OBSERVATION_PERFORMER_REFERENCE_INVALID = 226,
        FHIR_LOCATION_ADDRESS_MISSING = 301,
        FHIR_LOCATION_ADDRESS_COUNTRY_EMPTY = 302,
        FHIR_LOCATION_ADDRESS_COUNTRY_NOTONISOLIST = 303,
        FHIR_PERFORMER_ADDRESS_MISSING = 304,
        FHIR_PERFORMER_ADDRESS_COUNTRY_EMPTY = 305,
        FHIR_PERFORMER_ADDRESS_COUNTRY_NOTONISOLIST = 306,
        FHIR_PERFORMER_NAME_MISSING = 307,
        FHIR_DEVICE_MISSING = 400,
        FHIR_DEVICE_IDENTIFIER_MISSING = 401,
        FHIR_DEVICE_LOINC_IDENTIFIER_MISSING = 402,
        FHIR_DEVICE_LOINC_IDENTIFIER_INVALID = 403,
        FHIR_DEVICE_DEVICENAME_MISSING = 404,
        FHIR_DEVICE_DEVICENAME_NAME_MISSING = 405,
        FHIR_DEVICE_MANUFACTURER_MISSING = 406,
        FHIR_DEVICE_RAT_IDENTIFIER_MISSING = 407,
        FHIR_DEVICE_RAT_IDENTIFIER_INVALID = 408
    }
}
