namespace CovidCertificate.Backend.Interfaces.ManagementInformation
{
    public interface IManagementInformationReportingService
    {
        void AddReportLogInformation(string message, string odsCountry, string reportingStatus);
        void AddReportLogInformation(string message, string odsCountry, string reportingStatus, int ageInYears);
        void AddReportLogMedicalExemptionInformation(string message, string exemptionType, string exemptionReason);
    }
}
