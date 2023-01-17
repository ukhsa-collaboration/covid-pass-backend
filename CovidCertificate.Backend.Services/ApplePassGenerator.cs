using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.BlobService;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Passbook.Generator;
using Passbook.Generator.Fields;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels.PassData;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend.Services
{
    public class ApplePassGenerator : IGenerateApplePass
    {
        private readonly IConfiguration configuration;
        private readonly ICovidCertificateService covidCertificateCreator;
        private readonly IBlobService blobService;
        private readonly ILogger<ApplePassGenerator> logger;
        private readonly IGeneratePassData generatePassData;
        private readonly IHtmlGeneratorService htmlGenerator;
        private readonly IFeatureManager featureManager;
        private readonly PassSettings settings;
        private readonly string secretPassCert = default;
        private readonly string PassTypeIdentifier = "pass.uk.gov.dhsc.healthrecord";
        private readonly string TeamIdentifier = "877YMUFLMF";
        private readonly string WhiteHex = "#FFFFFF";
        private readonly string AllowedCertificateTypes;


        public ApplePassGenerator(IConfiguration configuration, ICovidCertificateService covidCertificateCreator, IBlobService blobService,
            ILogger<ApplePassGenerator> logger, PassSettings settings, IGeneratePassData passData, IHtmlGeneratorService htmlGenerator, IFeatureManager featureManager)
        {
            this.configuration = configuration;
            this.covidCertificateCreator = covidCertificateCreator;
            this.blobService = blobService;
            this.logger = logger;
            this.settings = settings;
            this.generatePassData = passData;
            this.htmlGenerator = htmlGenerator;
            this.featureManager = featureManager;
            secretPassCert ??= configuration["AppleWalletPassCert"];
            AllowedCertificateTypes = configuration["AllowedAppleCertTypes"];
        }

        public async Task<MemoryStream> GeneratePassAsync(CovidPassportUser covidPassportUser, QRType qrType, string languageCode, string idToken = "", int doseNumber = 0)
        {
            logger.LogTraceAndDebug("GeneratePass was invoked");
            if (!AllowedPassType(qrType))
            {
                throw new Exception("Disabled Pass Type");
            }
            var generator = new PassGenerator();
            var request = await PrepareRequestObjectAsync(covidPassportUser, idToken, qrType, doseNumber, languageCode);
            var generatedPass = generator.Generate(request);
            var memoryStream = new MemoryStream(generatedPass);
            logger.LogTraceAndDebug("GeneratePass has finished");
            return memoryStream;
        }

        // extracted this functionality to make more testable.
        // unit tests done on this method, the above method is trusted code
        public async Task<PassGeneratorRequest> PrepareRequestObjectAsync(CovidPassportUser covidTestUser, string idToken, QRType type, int doseNumber, string languageCode)
        {
            logger.LogTraceAndDebug("PrepareRequestObject was invoked");
            var request = new PassGeneratorRequest();
            var data = await generatePassData.GetPassDataAsync(covidTestUser, idToken, type, NhsdApiKey.Attended, languageCode);
            var qrCode = data.qr;
            var cert = data.cert;
            if (qrCode == null || cert == null)
            {
                throw new InvalidDataException("Invalid pass type");
            }
            var expiryText = (await htmlGenerator.GetPassTermsAsync(languageCode)).Replace("DEVICE", "Apple");
            var passLabels = await htmlGenerator.GetPassLabelsAsync(languageCode);
            request = await PrepareCommonFieldsAsync(request, covidTestUser, passLabels,languageCode);
            request.AddSecondaryField(new StandardField("Expiry2", passLabels["Expiry2Apple"], qrCode.ValidityEndDate));
            request.ExpirationDate = cert.ValidityEndDate;
            switch (type)
            {
                case QRType.Domestic:
                    request = await PrepareDomesticPassAsync(request, cert, passLabels);
                    break;
                case QRType.International:
                    request = await PrepareInternationalPassAsync(request, cert, doseNumber, passLabels, languageCode);
                    break;
                case QRType.Recovery:
                    request = await PrepareRecoveryPassAsync(request, cert, passLabels, languageCode);
                    break;
                default:
                    logger.LogError("Unknown QR Type");
                    throw new InvalidDataException("Unknown QR Type");
            }
            request.AddBackField(new StandardField("Expiry3", passLabels["Expiry3"], expiryText));
            request.TransitType = TransitType.PKTransitTypeAir;
            logger.LogTraceAndDebug("PrepareRequestObject has finished");

            return request;
        }

        private async Task<PassGeneratorRequest> PrepareCommonFieldsAsync(PassGeneratorRequest request, CovidPassportUser covidTestUser, Dictionary<string, string> passLabels,string languageCode)
        {
            request = AddPassSettings(request);
            request.PassbookCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(Convert.FromBase64String(secretPassCert));
            request.AppleWWDRCACertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(await blobService.GetImageFromBlobWithRetryAsync("pubkeys", "AppleWWDRCA.cer"));
            request = AddPassImages(request, await blobService.GetImageFromBlobWithRetryAsync("images", "nhs_covid_status_eighth.png"));
            request.Style = PassStyle.Generic;
            request.SharingProhibited = true;
            request.AddPrimaryField(new StandardField("Name", passLabels["Name"], covidTestUser.Name.ToUpper()));
            request.AddSecondaryField(new StandardField("DateOfBirth", passLabels["DateOfBirth"], StringUtils.GetTranslatedAndFormattedDate(covidTestUser.DateOfBirth,languageCode)));

            return request;
        }

        private async Task<PassGeneratorRequest> PrepareDomesticPassAsync(PassGeneratorRequest request, Certificate cert, Dictionary<string, string> passLabels)
        {
            request.SerialNumber = "0";
            request.LogoText = passLabels["DomesticLogo"];
            request.Description = passLabels["DomesticDescription"];
            request.AddBarcode(BarcodeType.PKBarcodeFormatQR, cert.QrCodeTokens[0], "UTF-8");
       
            var voluntaryDomesticOn = await featureManager.IsEnabledAsync(FeatureFlags.VoluntaryDomestic);

            if ((cert.CertificateType == CertificateType.DomesticMandatory) && voluntaryDomesticOn)
            {
                request.BackgroundColor = settings.BackgroundColourMandatory;
                request.AddAuxiliaryField(new StandardField("Validity", passLabels["Validity"], passLabels["MandatoryValidityTwoPass"]));
            }
            else if ((cert.CertificateType == CertificateType.DomesticVoluntary) && voluntaryDomesticOn)
            {
                request.BackgroundColor = settings.BackgroundColourVoluntary;
                request.AddAuxiliaryField(new StandardField("Validity", passLabels["Validity"], passLabels["VoluntaryValidity"]));
            }
            else
            {
                request.BackgroundColor = settings.BackgroundColourDomestic;
            }
            return request;
        }

        private async Task<PassGeneratorRequest> PrepareInternationalPassAsync(PassGeneratorRequest request, Certificate certificate, int doseNumber, Dictionary<string, string> passLabels, string languageCode)
        {
            try
            {
                var vaccine = certificate.GetAllVaccinationsFromEligibleResults().OrderBy(x => x.VaccinationDate).ToArray()[doseNumber];
                var passData = new VaccinePassData(vaccine,passLabels,languageCode);
                passData.Uvci = certificate.UniqueCertificateIdentifier;
                var vaccineFieldData = await htmlGenerator.GetPassHtmlAsync(passData, PassData.vaccine, languageCode);
                var vaccineFieldDataFinal = vaccineFieldData.Replace("\\n", "\n");
                request.AddBackField(new StandardField("Vaccine 1", passData.Product, vaccineFieldDataFinal));
                request.AddAuxiliaryField(new StandardField("Manufacturer", passLabels["Product"], passData.Product));
                request.AddAuxiliaryField(new StandardField("DateOfDose", passLabels["DateOfDose"] + " " + vaccine.DoseNumber, passData.VaccinationDate));
                request.AddHeaderField(new StandardField("DoseNumber", vaccine.IsBooster ? passLabels["DoseNumberBooster"]: passLabels["DoseNumber"], vaccine.DoseNumber.ToString()));
                certificate.QrCodeTokens.Reverse();
                request.AddBarcode(BarcodeType.PKBarcodeFormatQR, certificate.QrCodeTokens[doseNumber], "UTF-8");
                request.SerialNumber = vaccine.DoseNumber + vaccine.VaccinationDate.ToString(CultureInfo.InvariantCulture);
                request.LogoText = passLabels["International/RecoveryLogo"];
                request.Description = passLabels["International/RecoveryDescription"];
                request.BackgroundColor = settings.BackgroundColourInternational;
                logger.LogTraceAndDebug("International Pass is generated");
                return request;
            }
            catch (IndexOutOfRangeException)
            {
                logger.LogError("Dose number: " + doseNumber + " cannot be found");
                throw new InvalidDataException("Vaccine Dose does not exist");
            }
        }

        private async Task<PassGeneratorRequest> PrepareRecoveryPassAsync(PassGeneratorRequest request, Certificate certificate, Dictionary<string, string> passLabels, string languageCode)
        {
            var result = certificate.GetLatestDiagnosticResultFromEligibleResultsOrDefault();
            var passData = new RecoveryPassData(result,languageCode);
            passData.CertificateValidUntil = StringUtils.GetTranslatedAndFormattedDate(certificate.ValidityEndDate,languageCode);
            passData.Uvci = certificate.UniqueCertificateIdentifier;
            var recoveryFieldData = await htmlGenerator.GetPassHtmlAsync(passData, PassData.recovery, languageCode);
            var recoveryFieldDataFinal = recoveryFieldData.Replace("\\n", "\n");
            request.AddBackField(new StandardField("Recovery 1", passLabels["RecoveryWarning"], recoveryFieldDataFinal));
            request.AddAuxiliaryField(new StandardField("status", passLabels["Status"], passLabels["Recovery"]));
            request.LogoText = passLabels["International/RecoveryLogo"];
            request.Description = passLabels["International/RecoveryDescription"];
            request.SerialNumber = "99";
            request.BackgroundColor = settings.BackgroundColourInternational;
            request.AddBarcode(BarcodeType.PKBarcodeFormatQR, certificate.QrCodeTokens[0], "UTF-8");
            logger.LogTraceAndDebug("Recovery Pass is generated");
            return request;
        }

        private PassGeneratorRequest AddPassSettings(PassGeneratorRequest request)
        {
            request.PassTypeIdentifier = PassTypeIdentifier;
            request.TeamIdentifier = TeamIdentifier;
            request.OrganizationName = settings.PassOrigins;
            request.LabelColor = WhiteHex;
            request.ForegroundColor = WhiteHex;
            return request;
        }
        private PassGeneratorRequest AddPassImages(PassGeneratorRequest request, byte[] image)
        {
            request.Images.Add(PassbookImage.Icon, image);
            request.Images.Add(PassbookImage.Icon2X, image);
            request.Images.Add(PassbookImage.Icon3X, image);
            request.Images.Add(PassbookImage.Logo, image);
            request.Images.Add(PassbookImage.Logo2X, image);
            request.Images.Add(PassbookImage.Logo3X, image);
            return request;
        }

        private bool AllowedPassType(QRType qRType)
        {
            var lstPassTypes = new List<string>();
            if (!string.IsNullOrEmpty(AllowedCertificateTypes))
            {
                var types = AllowedCertificateTypes.Split(';');
                lstPassTypes.AddRange(types);
            }
            return lstPassTypes.Contains(qRType.ToString());
        }
    }
}
