using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Utils.Extensions;

namespace CovidCertificate.Backend.Services
{
    public class GeneratePassData : IGeneratePassData
    {
        private readonly ILogger<GeneratePassData> logger;
        private readonly ICovidCertificateService covidCertificateCreator;
        private readonly ICovidResultsService covidResultsService;
        private readonly TimeZoneInfo timeZoneInfo;

        public GeneratePassData(ILogger<GeneratePassData> logger,
            IGetTimeZones timeZones,
            ICovidCertificateService covidCertificateCreator,
            ICovidResultsService covidResultsService)
        {
            this.logger = logger;
            this.covidCertificateCreator = covidCertificateCreator;
            this.covidResultsService = covidResultsService;
            timeZoneInfo = timeZones.GetTimeZoneInfo();
        }

        public async Task<(QRcodeResponse qr,Certificate cert)> GetPassDataAsync(CovidPassportUser user, string idToken, QRType type, string apiKey, string languageCode)
        {
            var scenario = type == QRType.Domestic ? CertificateScenario.Domestic : CertificateScenario.International;
            var medicalResults = await covidResultsService.GetMedicalResultsAsync(user, idToken, scenario, apiKey);

            switch (type)
            {
                case QRType.Domestic:
                    var domesticCertContainer = await covidCertificateCreator.GetDomesticCertificateAsync(user, idToken, medicalResults);
                    var domesticCert = domesticCertContainer.GetSingleCertificateOrNull();

                    return CreatePassData(domesticCert, QRResponseType.Domestic, languageCode);

                case QRType.International:
                    var vaccinationCertificateContainer = await covidCertificateCreator.GetInternationalCertificateAsync(user, idToken, CertificateType.Vaccination, medicalResults);
                    var vaccinationCertificate = vaccinationCertificateContainer.GetSingleCertificateOrNull();

                    return CreatePassData(vaccinationCertificate, QRResponseType.Vaccination, languageCode);

                case QRType.Recovery:
                    var recoveryCertificateContainer = await covidCertificateCreator.GetInternationalCertificateAsync(user, idToken, CertificateType.Recovery, medicalResults);
                    var recoveryCertificate = recoveryCertificateContainer.GetSingleCertificateOrNull();

                    return CreatePassData(recoveryCertificate, QRResponseType.Recovery, languageCode);

                default:
                    return (null,null);
            }
        }

        private (QRcodeResponse, Certificate) CreatePassData(Certificate certificate, QRResponseType qrResponseType, string languageCode)
        {
            if (certificate == null)
                throw new NoResultsException($"No {qrResponseType} certificate to generate {qrResponseType} QR");

            var validityEndDate = GetValidityEndDateString(qrResponseType, certificate.ValidityEndDate, languageCode);

            var QRResponse = new QRcodeResponse(validityEndDate,
                                            certificate.EligibilityResults,
                                            certificate.UniqueCertificateIdentifier,
                                            qrResponseType,
                                            StringUtils.GetTranslatedAndFormattedDate(TimeZoneInfo.ConvertTimeFromUtc(certificate.eligibilityEndDate, timeZoneInfo), languageCode));

            return (QRResponse, certificate);
        }

        private string GetValidityEndDateString(QRResponseType qrResponseType, DateTime validityEndDate, string languageCode)
        {
            if (qrResponseType == QRResponseType.Domestic)
            {
                return StringUtils.GetTranslatedAndFormattedDateTime(TimeZoneInfo.ConvertTimeFromUtc(validityEndDate, timeZoneInfo), languageCode);
            }

            return StringUtils.GetTranslatedAndFormattedDate(TimeZoneInfo.ConvertTimeFromUtc(validityEndDate, timeZoneInfo), languageCode);
        }
    }
}
