using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Settings;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using CovidCertificate.Backend.Models.DataModels.PassData;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.FeatureManagement;

namespace CovidCertificate.Backend.Services
{
    public class GooglePassJwt : IGooglePassJwt
    {

        private readonly ILogger<GooglePassJwt> _logger;
        private readonly PassSettings settings;
        private readonly IGeneratePassData generatePassData;
        private readonly string privateKey;
        private readonly IHtmlGeneratorService htmlGenerator;
        private readonly IFeatureManager featureManager;
        private readonly string AllowedCertificateTypes;
        public GooglePassJwt(ILogger<GooglePassJwt> logger, 
            IGeneratePassData passData, 
            IConfiguration configuration, 
            PassSettings passSettings, 
            IHtmlGeneratorService htmlGenerator,
            IFeatureManager featureManager)
        {
            _logger = logger;
            AllowedCertificateTypes = configuration["AllowedGoogleCertTypes"];
            generatePassData = passData;
            this.htmlGenerator = htmlGenerator;
            settings = passSettings;
            this.privateKey = configuration["GooglePrivateKey"];
            this.featureManager = featureManager;
        }

        public async Task<string> GenerateJwtAsync(CovidPassportUser user, QRType qrType, string languageCode, int doseNumber, string apiKey, string idToken = "")
        {
            _logger.LogTraceAndDebug("GenerateJwt was invoked");
            if (!AllowedPassType(qrType))
            {
                throw new Exception("Disabled Pass Type");
            }
            var data = await generatePassData.GetPassDataAsync(user, idToken, qrType, apiKey, languageCode);
            var expiryText = (await htmlGenerator.GetPassTermsAsync(languageCode)).Replace("DEVICE", "Google");
            var passLabels = await htmlGenerator.GetPassLabelsAsync(languageCode);
            passLabels.Add("ExpiryText", expiryText);
            var qrCode = data.qr;
            var cert = data.cert;
            if (qrCode == null || cert == null)
            {
                throw new InvalidDataException("Invalid pass type");
            }
            var covidCardObject = PrepareCommonFields(user,passLabels,languageCode);
            switch (qrType)
            {
                case QRType.Domestic:
                    await PrepareDomesticFieldsAsync(covidCardObject, cert, qrCode, passLabels);
                    break;
                case QRType.International:
                    await PrepareInternationalFieldsAsync(covidCardObject, cert, doseNumber, user, qrCode, passLabels, languageCode);
                    break;
                case QRType.Recovery:
                    await PrepareRecoveryFieldsAsync(covidCardObject, cert, user, qrCode, passLabels, languageCode);
                    break;
                default:
                    _logger.LogWarning("Unknown QR Type");
                    break;
            }
            var createPayload = new Payload
            {
                covidCardObjects = new List<CovidCardObject> { covidCardObject }
            };
            var privateKeyNew = privateKey.Replace("\\n", "\n");
            using RSA rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyNew), out _);
            var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };
            var jwtHeader = new JwtHeader(
                signingCredentials: signingCredentials);
            var jwtPayload = new JwtPayload
            {
                { "iss",settings.Iss},
                { "aud",settings.Audience},
                { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()},
                {"typ", settings.JwtType },
                {"payload",createPayload },
            };
            var jwt = new JwtSecurityToken(jwtHeader, jwtPayload);
            var handler = new JwtSecurityTokenHandler();
            var tokenString = handler.WriteToken(jwt);
            return tokenString;
        }

        private CovidCardObject PrepareCommonFields(CovidPassportUser user, Dictionary<string, string> passLabels,string languageCode)
        {
            var covidCardObject = new CovidCardObject();
            StringBuilder sb = new StringBuilder();
            sb.Append(settings.IssuerId).Append('.').Append(settings.UniqueId);
            covidCardObject.id = sb.ToString();
            covidCardObject.issuerId = settings.IssuerId.ToString();
            covidCardObject.barcode = new Barcode { type = "qrCode" };
            covidCardObject.barcode.alternateText = "Barcode";
            covidCardObject.patientDetails = new PatientDetails { patientName = user.Name, patientNameLabel= passLabels["Name"], dateOfBirth = StringUtils.GetTranslatedAndFormattedDate(user.DateOfBirth,languageCode),dateOfBirthLabel = passLabels["DateOfBirth"]};
            covidCardObject.title = settings.PassName;
            return covidCardObject;
        }

        private async Task<CovidCardObject> PrepareDomesticFieldsAsync(CovidCardObject pass, Certificate cert, QRcodeResponse code, Dictionary<string, string> passLabels)
        {
            pass.barcode.value = cert.QrCodeTokens[0];        
            pass.expiration = code.ValidityEndDate;
            pass.expirationLabel = passLabels["Expiry2Google"];
            pass.title = passLabels["DomesticDescription"];
            pass.cardDescription = passLabels["ExpiryText"];
            pass.cardDescriptionLabel = passLabels["Expiry3"];
            pass.logo = new Logo { sourceUri = new SourceUri { uri = "https://stordevcovidpass.blob.core.windows.net/images/nhs_blue.png", description = "NHS App" } };
            
            var voluntaryDomesticOn = await featureManager.IsEnabledAsync(FeatureFlags.VoluntaryDomestic);

            if ((cert.CertificateType == CertificateType.DomesticMandatory) && voluntaryDomesticOn)
            {
                pass.cardColorHex = settings.BackgroundColourMandatory;
                pass.summary = passLabels["MandatoryValidityTwoPass"];
            }
            else if ((cert.CertificateType == CertificateType.DomesticVoluntary) && voluntaryDomesticOn)
            {
                pass.cardColorHex = settings.BackgroundColourVoluntary;
                pass.summary = passLabels["VoluntaryValidity"];
            }
            else
            {
                pass.cardColorHex = settings.BackgroundColourDomestic;
            }
            _logger.LogTraceAndDebug("Domestic pass generated");
            return pass;
        }

        private async Task<CovidCardObject> PrepareInternationalFieldsAsync(CovidCardObject pass, Certificate cert, int doseNumber, CovidPassportUser user, QRcodeResponse code, Dictionary<string, string> passLabels, string languageCode)
        {
            try
            {
                var vaccine = cert.GetAllVaccinationsFromEligibleResults().OrderBy(x => x.VaccinationDate).ToArray()[doseNumber];
                cert.QrCodeTokens.Reverse();
                var passData = new VaccinePassData(vaccine,passLabels,languageCode);
                var vaccinationRecords = new List<VaccinationRecord>();
                var vaccinationDetails = await htmlGenerator.GetPassHtmlAsync(passData, PassData.vaccine, languageCode);
                var vaccinationDetailsFinal = vaccinationDetails.Replace("\\n", "\n");
                vaccinationRecords.Add(new VaccinationRecord { doseDateTime = passData.VaccinationDate, doseLabel = $"{passLabels["DoseNumberGoogle"]} {vaccine.DoseNumber} {passLabels["of"]} {vaccine.TotalSeriesOfDoses}", manufacturer = passData.Product, manufacturerLabel = passLabels["Product"] });
                pass.vaccinationDetails = new VaccinationDetails { vaccinationRecord = vaccinationRecords };
                pass.barcode.value = cert.QrCodeTokens[doseNumber];
                pass.expiration = code.ValidityEndDate;
                pass.expirationLabel = passLabels["Expiry2Google"];
                pass.cardDescription = vaccinationDetailsFinal + " \n\n" + passLabels["ExpiryText"];
                pass.cardDescriptionLabel = passData.Product;
                pass.title = passLabels["International/RecoveryDescription"];
                var doseLabel = vaccine.IsBooster ? passLabels["DoseNumberBooster"] : passLabels["DoseNumber"];
                pass.summary = $"{doseLabel} {vaccine.DoseNumber} {passLabels["of"]} {vaccine.TotalSeriesOfDoses}";
                pass.cardColorHex = settings.BackgroundColourInternational;
                pass.logo = new Logo { sourceUri = new SourceUri { uri = "https://stordevcovidpass.blob.core.windows.net/images/nhs_dark_blue.png", description = "NHS App" } };
                _logger.LogInformation("International vaccine pass generated");
                return pass;
            }
            catch (IndexOutOfRangeException)
            {
                _logger.LogError("Dose number: " + doseNumber + " cannot be found");
                throw new InvalidDataException("Vaccine Dose does not exist");
            }
        }

        private async Task<CovidCardObject> PrepareRecoveryFieldsAsync(CovidCardObject pass, Certificate cert, CovidPassportUser user, QRcodeResponse code, Dictionary<string, string> passLabels, string languageCode)
        {
            var result = cert.GetLatestDiagnosticResultFromEligibleResultsOrDefault();
            var passData = new RecoveryPassData(result,languageCode);
            passData.CertificateValidUntil = StringUtils.GetTranslatedAndFormattedDate(cert.ValidityEndDate,languageCode);
            var testDetails = await htmlGenerator.GetPassHtmlAsync(passData, PassData.recovery, languageCode);
            var testDetailsFinal = testDetails.Replace("\\n", "\n");
            pass.barcode.value = cert.QrCodeTokens[0];
            pass.expiration = code.ValidityEndDate;
            pass.expirationLabel = passLabels["Expiry2Google"];
            pass.title = passLabels["International/RecoveryDescription"];
            pass.cardDescription = testDetailsFinal + " \n\n" + passLabels["ExpiryText"];
            pass.cardDescriptionLabel = passLabels["RecoveryWarning"];
            pass.summary = passLabels["Recovery"];
            pass.cardColorHex = settings.BackgroundColourInternational;
            pass.logo = new Logo { sourceUri = new SourceUri { uri = "https://stordevcovidpass.blob.core.windows.net/images/nhs_dark_blue.png", description = "NHS App" } };
            _logger.LogInformation("International recovery pass generated");
            return pass;
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

