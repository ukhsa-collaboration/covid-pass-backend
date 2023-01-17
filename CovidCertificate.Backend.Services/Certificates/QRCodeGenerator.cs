using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.FeatureManagement;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Services.International;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class QRCodeGenerator : IQRCodeGenerator
    {
        private readonly ILogger<QRCodeGenerator> logger;
        private readonly IKeyRing keyRing;
        private readonly ICBORFlow cBORFlow;
        private readonly DomesticQRValues values;
        private readonly DomesticPolicy policy;
        private readonly IFeatureManager featureManager;
        private readonly IEncoderService encoder;

        public QRCodeGenerator(ILogger<QRCodeGenerator> logger,
            IKeyRing keyRing,
            DomesticQRValues values,
            DomesticPolicy policy,
            ICBORFlow cBORFlow,
            IFeatureManager featureManager,
            IEncoderService encoder
            )
        {
            this.logger = logger;
            this.keyRing = keyRing;
            this.values = values;
            this.policy = policy;
            this.cBORFlow = cBORFlow;
            this.featureManager = featureManager;
            this.encoder = encoder;
        }

        public async Task<List<string>> GenerateQRCodesAsync(Certificate certificate, CovidPassportUser user, string barcodeIssuerCountry = "")
        {
            logger.LogTraceAndDebug("GenerateQRCodesAsync was invoked");
            var scenario = certificate.CertificateScenario;
            switch (scenario){ 
                case CertificateScenario.Domestic:
                    return await GenerateDomesticQRCodesAsync(certificate, user, barcodeIssuerCountry);
                case CertificateScenario.International:
                    return await GenerateInternationalQrCodesAsync(certificate, user);
                default:
                    throw new QRCodeTypeException("QR Code of type " + scenario.ToString() + " not supported");
            }
                
        }
        
        private async Task<List<string>> GenerateDomesticQRCodesAsync(Certificate certificate, CovidPassportUser user, string barcodeIssuerCountry = "")
        {
            logger.LogTraceAndDebug("GenerateDomesticQRCodesAsync was invoked");

            var certificateTag = certificate.PKICountry == "GB" ? "DSC-ENG-WAL" : $"DSC-{certificate.PKICountry}";
            var keyId = string.IsNullOrWhiteSpace(certificate.PKICountry) ? await keyRing.GetRandomKeyAsync() : await keyRing.GetKeyByTagAsync(certificateTag);

            logger.LogTraceAndDebug("QR Code CBOR generated");

            return new List<string>() { await GenerateQRCBORAsync(certificate, user, keyId, barcodeIssuerCountry) };
        }

        private async Task<string> GenerateQRCBORAsync(Certificate certificate, CovidPassportUser user, string keyId, string barcodeIssuerCountry ="")
        {
            var JSON = JsonConvert.SerializeObject(GenerateQRObject(certificate, user, barcodeIssuerCountry));
            byte[] CBORBytes = CBORUtils.JsonToCbor(JSON);
            byte[] alteredBytes = await cBORFlow.AddMetaDataToCborAsync(CBORBytes, keyId, certificate.PKICountry);
            byte[] compressedBytes = await ZlibCompression.CompressData(alteredBytes);
            string encoded = Base45Encoding.Encode(compressedBytes);
            return $"HC1:{encoded}";
        }

        private QRCodeLayer GenerateQRObject(Certificate certificate, CovidPassportUser user, string barcodeIssuerCountry ="")
        {
            QRCodeLayer result = new QRCodeLayer();
            result.barcodeIssuerCountry = String.IsNullOrEmpty(barcodeIssuerCountry) ? "GB" : barcodeIssuerCountry;
            result.DateOfIssue = DateTimeOffset.Now.ToUnixTimeSeconds();
            var expiryOffset = new DateTimeOffset(certificate.ValidityEndDate);
            result.DateOfExpiry = expiryOffset.ToUnixTimeSeconds();

            DomesticQRCode content = new DomesticQRCode
            {
                DateOfBirth = user.DateOfBirth.Date.ToString("yyyy-MM-dd"),
                Version = values.Version,
                Name = ToNameJSON(user),
                Certificates = new List<CertModel> { GenerateCertJSON(certificate) }
            };
            result.content.code = content;
            return result;
        }

        private CertModel GenerateCertJSON(Certificate certificate)
        {
            CertModel model = new CertModel();
            model.UVCI = certificate.UniqueCertificateIdentifier;
            model.Country = String.IsNullOrEmpty(certificate.Country) ? values.Created : certificate.Country;
            model.Issuer = String.IsNullOrEmpty(certificate.Issuer) ? values.Issuer : certificate.Issuer;
            DateTime utcNow = DateTime.UtcNow;
            model.DateFrom = certificate.ValidityStartDate == null ? new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, utcNow.Second) : (DateTime)certificate.ValidityStartDate;
            model.DateUntil = certificate.ValidityEndDate;
            model.CertificateType = certificate.PolicyMask ?? values.CertificateType;
            string[] MandatoryCertOn = { policy.PlanB };
            model.Policy = certificate.Policy ?? MandatoryCertOn;

            return model;
        }

        private static Name ToNameJSON(CovidPassportUser user)
        {
            if (string.IsNullOrEmpty(user.GivenName) || string.IsNullOrEmpty(user.FamilyName))
                return GetNameByParsingFullName(user);
            return GetNameFromFamilyAndGivenNames(user);
        }

        private static Name GetNameFromFamilyAndGivenNames(CovidPassportUser user)
        {
            var userNamesObj = new Name();
            userNamesObj.Surname = user.FamilyName;
            userNamesObj.Forename = user.GivenName;

            var familyNameUpper = user.FamilyName?.ToUpper() ?? "";
            var givenNameUpper = user.GivenName?.ToUpper() ?? "";

            var transliterationResults = GetTransliterationResults(familyNameUpper, givenNameUpper);

            return GetNameFromRegex(userNamesObj, transliterationResults);
        }

        private static Name GetNameFromRegex(Name userNamesObj, List<string> transliterationResults)
        {
            var fnt = transliterationResults[0].Replace("-", "<").Replace(" ", "<").Replace("'", "<");
            var gnt = transliterationResults[1].Replace("-", "<").Replace(" ", "<").Replace("'", "<");

            var regex = new Regex(@"^[A-Z<]*$");
            if (regex.IsMatch(fnt) && regex.IsMatch(gnt))
            {
                userNamesObj.SurnameStandardised = fnt;
                userNamesObj.ForenameStandardised = gnt;
            }
            else
            {
                userNamesObj.SurnameStandardised = transliterationResults[0];
                userNamesObj.ForenameStandardised = transliterationResults[1];
            }
            return userNamesObj;
        }

        private static List<string> GetTransliterationResults(string familyNameUpper, string givenNameUpper)
        {
            var transliterationList = TransliterationModelList.TransliterationList();
            var replaceArray = familyNameUpper.ToArray();
            var result = new List<string>();

            for (var j = 0; j < 2; j++) //We are running through this once for the familyname and once for the givenname
            {
                for (var i = 0; i < replaceArray.Length; i++)
                {
                    var unicodeCheck = $"{(int)replaceArray[i]:x4}";
                    if (!transliterationList.Any(x =>
                        x.Unicode.Contains(unicodeCheck, StringComparison.OrdinalIgnoreCase))) continue;
                    {
                        var transliterationSelect = transliterationList.Where(x => x.Unicode.Contains(unicodeCheck, StringComparison.OrdinalIgnoreCase))
                            .Select(m => m.RecommendedTransliteration);
                        var toCharArray = transliterationSelect.First().ToCharArray();
                        replaceArray[i] = toCharArray[0];
                    }
                }
                result.Add(new string(replaceArray));
                replaceArray = givenNameUpper.ToArray();
            }

            return result;
        }

        private static Name GetNameByParsingFullName(CovidPassportUser user)
        {
            var userNamesObj = new Name();
            var fullName = user.Name.Split(' ');

            var firstAndMiddleNames = string.Join(' ', fullName.Take(fullName.Length - 1));
            var lastName = fullName.Last();
            var transliterationResults = GetTransliterationResults(lastName.ToUpper(), firstAndMiddleNames.ToUpper());

            userNamesObj.Surname = lastName.ToLower();
            userNamesObj.Forename = firstAndMiddleNames.ToLower();


            return GetNameFromRegex(userNamesObj, transliterationResults);
        }


        private async Task<List<string>> GenerateInternationalQrCodesAsync(Certificate certificate, CovidPassportUser user)
        {
            var qrCodes = new List<string>();
            if (certificate.CertificateType == CertificateType.Recovery)
            {
                qrCodes.Add(await EncodeInternationalQrAsync(certificate, user, 0));
            }
            else
            {
                for (int i = 0; i < certificate.EligibilityResults.Count(); i++)
                {
                    qrCodes.Add(await EncodeInternationalQrAsync(certificate, user, i));
                }
            }
            return qrCodes;
        }

        private async Task<string> EncodeInternationalQrAsync(Certificate certificate, CovidPassportUser user, int resultIndex)
        {
            var dateTimeOffset = DateTimeOffset.Now.ToUnixTimeSeconds();
            IGenericResult result;
            if (certificate.CertificateType.Equals(CertificateType.Vaccination))
                result = certificate.GetAllVaccinationsFromEligibleResults().ElementAt(resultIndex);
            else
                result = certificate.GetLatestDiagnosticResultFromEligibleResultsOrDefault();
            return await encoder.EncodeFlowAsync(user, dateTimeOffset, result, certificate.UniqueCertificateIdentifier, certificate.ValidityEndDate);
        }
    }
}
