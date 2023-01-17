using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class CovidCertificateBuilder : ICovidCertificateBuilder
    {
        private static readonly List<string> gbCountries = new List<string> { "GB", "IM", "JE", "GG" };

        private readonly ILogger<CovidCertificateBuilder> logger;
        private readonly IEncoderService encoder;
        private readonly IBlobFilesInMemoryCache<EligibilityConfiguration> eligibilityConfigurationBlobCache;
        private readonly IConfigurationValidityCalculator configurationValidityCalculator;
        private readonly IGetTimeZones getTimeZones;
        private readonly IQRCodeGenerator qRCodeGenerator;
        private readonly IUVCIGeneratorService uvciGenerator;
        private readonly IConfiguration configuration;
        private readonly IEligibilityConfigurationService eligibilityConfigurationService;
        private readonly TelemetryClient telemetryClient;

        public CovidCertificateBuilder(IEncoderService encoder, 
                                        ILogger<CovidCertificateBuilder> logger, 
                                        IBlobFilesInMemoryCache<EligibilityConfiguration> eligibilityConfigurationBlobCache, 
                                        IConfigurationValidityCalculator configurationValidityCalculator,
                                        IGetTimeZones getTimeZones,
                                        IQRCodeGenerator qRCodeGenerator,
                                        IUVCIGeneratorService uvciGenerator,
                                        IConfiguration configuration,
                                        IEligibilityConfigurationService eligibilityConfigurationService,
                                        TelemetryClient telemetryClient)
        {
            this.encoder = encoder;
            this.logger = logger;
            this.eligibilityConfigurationBlobCache = eligibilityConfigurationBlobCache;
            this.configurationValidityCalculator = configurationValidityCalculator;
            this.getTimeZones = getTimeZones;
            this.qRCodeGenerator = qRCodeGenerator;
            this.uvciGenerator = uvciGenerator;
            this.configuration = configuration;
            this.eligibilityConfigurationService = eligibilityConfigurationService;
            this.telemetryClient = telemetryClient;
        }

        public async Task<CertificatesContainer> BuildCertificatesFromResultsAsync(List<IGenericResult> allEligibilityData, 
                                                                            CovidPassportUser user, 
                                                                            CertificateScenario scenario, 
                                                                            CertificateType? type = null)
        {
            logger.LogTraceAndDebug("BuildCertificatesFromResultsAsync was invoked");

            //Get the curent configuration settings for the certificates
            var rules = await GetEligibilityRulesAsync(scenario);

            var certificatesToBuild = configurationValidityCalculator.GenerateCertificatesUsingRules(allEligibilityData, rules, user, DateTime.Now);

            if (type != null)
                certificatesToBuild = certificatesToBuild.Where(x => x.CertificateType.Equals(type));

            TrackCertificateCreationEvent(allEligibilityData, certificatesToBuild, scenario, type, user);

            if (certificatesToBuild.FirstOrDefault() != default)
            {
                foreach (var certificate in certificatesToBuild)
                {
                    certificate.Name = user.Name;
                    certificate.DateOfBirth = user.DateOfBirth;
                    certificate.UniqueCertificateIdentifier = await GetUvciAsync(certificate);
                    certificate.ConvertTimeZone(getTimeZones.GetTimeZoneInfo());
                    certificate.QrCodeTokens = await qRCodeGenerator.GenerateQRCodesAsync(certificate, user);
                }
            }

            logger.LogTraceAndDebug($"BuildCertificatesFromResultsAsync: {certificatesToBuild?.ToString()}");
            logger.LogTraceAndDebug("BuildCertificatesFromResultsAsync has finished");
            
            return new CertificatesContainer(certificatesToBuild);
        }

        private async Task<string> GetUvciAsync(Certificate certificate)
        {
            return await uvciGenerator.GenerateAndInsertUvciAsync(
                new GenerateAndInsertUvciCommand(
                    certificate.CertificateScenario.Equals(CertificateScenario.International) ? configuration["InternationalAuthority"] : configuration["DomesticAuthority"],
                    configuration["CountryOfIssuer"],
                    StringUtils.GetHashValue(certificate.Name, certificate.DateOfBirth),
                    certificate.CertificateType,
                    certificate.CertificateScenario,
                    certificate.ValidityEndDate));
        }

        private async Task<IEnumerable<EligibilityRules>> GetEligibilityRulesAsync(CertificateScenario scenario)
        {
            (var container, var filename) = await eligibilityConfigurationService.GetEligibilityConfigurationBlobContainerAndFilenameAsync();
            var configuration = await eligibilityConfigurationBlobCache.GetFileAsync(container, filename);
            return configuration.Rules != null ? configuration.Rules.Where(x => x.Scenario.Equals(scenario)) : Enumerable.Empty<EligibilityRules>();
        }
        private void TrackCertificateCreationEvent(IEnumerable<IGenericResult> allEligibilityData, IEnumerable<Certificate> certificates, CertificateScenario scenario, CertificateType? type, CovidPassportUser covidUser)
        {
            var passIssued = certificates.Any();
            var validVaccinations = passIssued ? certificates.First().GetAllVaccinationsFromEligibleResults() : new List<Vaccine>();

            var vaccinations = new List<Vaccine>();

            foreach(var result in allEligibilityData)
            {
                if(result is Vaccine)
                {
                    vaccinations.Add((Vaccine)result);
                }
            }

            var invalidVaccinations = vaccinations.Any() ? vaccinations.Except(validVaccinations) : new List<Vaccine>();

            var customProperties = new Dictionary<string, string>()
            {
                { "CertificateScenario", scenario.ToString() },
                { "CertificateType", type?.ToString() },
                { "PassIssued", passIssued.ToString() },
            };

            var vaccineCount = 1;
            foreach (var validVaccination in validVaccinations)
            {
                customProperties.Add($"Vaccine{vaccineCount}Name", validVaccination.DisplayName);
                customProperties.Add($"CountryCode{vaccineCount}Name", validVaccination.CountryCode);
                vaccineCount++;
            }

            foreach (var invalidVaccination in invalidVaccinations)
            {
                customProperties.Add($"Vaccine{vaccineCount}", invalidVaccination.DisplayName);
                customProperties.Add($"CountryCode{vaccineCount}", invalidVaccination.CountryCode);
                vaccineCount++;
            }

            var ageInYears = DateUtils.GetAgeInYears(covidUser.DateOfBirth);

            customProperties.Add("age", ageInYears + "");

            telemetryClient.TrackEvent("CertificateCreation", customProperties);
        }
    }
}
