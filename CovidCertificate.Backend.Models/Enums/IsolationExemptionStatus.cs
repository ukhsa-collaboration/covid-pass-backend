namespace CovidCertificate.Backend.Models.Enums
{
    public enum IsolationExemptionStatus
    {
        FULLY_VACCINATED = 1,
        MEDICAL_EXEMPTION = 2,
        CLINICAL_TRIAL = 3,
        INSUFFICIENT_TIME_SINCE_LAST_VACCINATION = 4,
        INSUFFICIENT_RECORDS_FOUND = 5
    }
}
