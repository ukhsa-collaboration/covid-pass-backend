using CovidCertificate.Backend.Interfaces.ManagementInformation;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.ManagementInformation
{
    public class ManagementInformationReportingService : IManagementInformationReportingService
    {
        private readonly ILogger logger;

        public ManagementInformationReportingService(ILogger<ManagementInformationReportingService> logger)
        {
            this.logger = logger;
        }

        public void AddReportLogInformation(string message, string odsCountry, string reportingStatus)
        {
            logger.LogInformation("{message}:{odsCountry}:{reportingStatus}", message, odsCountry, reportingStatus);
        }

        public void AddReportLogInformation(string message, string odsCountry, string reportingStatus, int ageInYears)
        {
            logger.LogInformation("{message}:{odsCountry}:{reportingStatus}:{ageInYears}", message, odsCountry, reportingStatus, ageInYears);
        }

        public void AddReportLogMedicalExemptionInformation(string message, string exemptionType, string exemptionReason)
        {
            logger.LogInformation("{message}:{exemptionType}:{exemptionReason}", message, exemptionType, exemptionReason);
        }
    }
}
