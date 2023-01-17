using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.Interfaces.UserInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PeterO.Cbor;

namespace CovidCertificate.Backend.Services.International
{
    public class CondensorService : ICondensorService
    {
        private readonly ILogger<CondensorService> logger;
        private readonly IConfiguration configuration;
        private readonly TimeZoneInfo timeZoneInfo;

        public CondensorService(ILogger<CondensorService> logger, IConfiguration configuration, IGetTimeZones timeZones)
        {
            this.logger = logger;
            this.configuration = configuration;
            timeZoneInfo = timeZones.GetTimeZoneInfo();
        }

        public CBORObject CondenseCBOR(IUserCBORInformation user, long certifiateGenerationTime, IGenericResult result,
            string uniqueCertificateIdentifier, DateTime? validityEndDate, string barcodeIssuerCountry = null)
        {
            try
            {
                CBORObject outsideLayer, euHcertV1SchemaLayer, cborArray;
                (outsideLayer, euHcertV1SchemaLayer, cborArray) = CreateCBORObjects(certifiateGenerationTime, barcodeIssuerCountry);

                switch (result)
                {
                    case Vaccine vaccine:
                        {
                            AddVaccineCBOR(uniqueCertificateIdentifier, validityEndDate,
                                outsideLayer, euHcertV1SchemaLayer, cborArray, vaccine);
                            break;
                        }

                    case TestResultNhs testResult:
                        {
                            AddTestResultCBOR(uniqueCertificateIdentifier, validityEndDate,
                                outsideLayer, euHcertV1SchemaLayer, cborArray, testResult);
                            break;
                        }
                }

                euHcertV1SchemaLayer.Add("dob", user.DateOfBirth.ToString("yyyy-MM-dd"));
                euHcertV1SchemaLayer.Add("nam", ToNameCBOR(user));
                euHcertV1SchemaLayer.Add("ver", configuration["SchemaVersion"]);

                return outsideLayer;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                throw new ArgumentException("Error in Condensor: " + e.Message);
            }
        }

        private void AddTestResultCBOR(string uniqueCertificateIdentifier, DateTime? validityEndDate,
            CBORObject outsideLayer, CBORObject euHcertV1SchemaLayer, CBORObject cborArray, TestResultNhs testResult)
        {
            string certificateTypeFieldName;
            if (IsTestResult(testResult))
            {
                //Test result
                cborArray.Add(ToTestResultCBOR(testResult, uniqueCertificateIdentifier));
                certificateTypeFieldName = "t";
            }
            else
            {
                //Recovery result
                cborArray.Add(ToRecoveryCBOR(testResult, validityEndDate, uniqueCertificateIdentifier));
                certificateTypeFieldName = "r";
            }
            
            AddCertificateInformation(validityEndDate, outsideLayer, euHcertV1SchemaLayer, cborArray, certificateTypeFieldName);
        }

        private void AddVaccineCBOR(string uniqueCertificateIdentifier, DateTime? validityEndDate, CBORObject outsideLayer,
            CBORObject euHcertV1SchemaLayer, CBORObject cborArray, Vaccine vaccine)
        {
            cborArray.Add(ToVaccineCBOR(vaccine, uniqueCertificateIdentifier));

            AddCertificateInformation(validityEndDate, outsideLayer, euHcertV1SchemaLayer, cborArray, "v");
        }

        private static void AddCertificateInformation(DateTime? validityEndDate, CBORObject outsideLayer,
            CBORObject euHcertV1SchemaLayer, CBORObject cborArray, string certType)
        {
            if (validityEndDate != null)
            {
                //From .NET 5.0 UTC time is used by default so no need for zero timespan
                var dateTimeOffset = new DateTimeOffset(validityEndDate.Value);
                // outsideLayer setup
                outsideLayer.Add(4, dateTimeOffset.ToUnixTimeSeconds());
            }

            // Please refer to https://github.com/ehn-digital-green-development/ehn-dgc-schema/blob/main/DGC.combined-schema.json
            // All fields key values are following official EU schema

            euHcertV1SchemaLayer.Add(certType, cborArray);
        }

        private (CBORObject outsideLayer, CBORObject euHcertV1SchemaLayer, CBORObject cborArray) CreateCBORObjects(
            long certifiateGenerationTime, string barcodeIssuerCountry)
        {
            var outsideLayer = CBORObject.NewMap();
            CBORObject hcertLayer = CBORObject.NewMap();
            var euHcertV1SchemaLayer = CBORObject.NewMap();
            // Please refer to https://ec.europa.eu/health/sites/default/files/ehealth/docs/digital-green-certificates_v1_en.pdf (Section 3.3.1)
            // Please do not change key value integers for CBOR maps and arrays
            // outsideLayer setup
            string field1Value = barcodeIssuerCountry != null ? barcodeIssuerCountry : configuration["CountryOfVaccination"];
            outsideLayer.Add(1, field1Value);
            outsideLayer.Add(6, certifiateGenerationTime); // Date of issue
            outsideLayer.Add(-260, hcertLayer);
            hcertLayer.Add(1, euHcertV1SchemaLayer);
            //euHcertV1SchemaLayer setup
            var cborArray = CBORObject.NewArray();

            return (outsideLayer, euHcertV1SchemaLayer, cborArray);
        }

        private CBORObject ToVaccineCBOR(Vaccine vaccine, string uniqueCertificateIdentifier)
        {
            // Please refer to https://github.com/ehn-digital-green-development/ehn-dgc-schema/blob/main/DGC.combined-schema.json
            // All fields key values are following official EU schema
            CBORObject vaccineObj = CBORObject.NewMap();
            vaccineObj.Add("ci", uniqueCertificateIdentifier);
            vaccineObj.Add("co", vaccine.CountryOfVaccination);
            vaccineObj.Add("dn", vaccine.DoseNumber);
            vaccineObj.Add("dt", TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(vaccine.VaccinationDate, DateTimeKind.Unspecified), timeZoneInfo).ToString("yyyy-MM-dd"));
            vaccineObj.Add("is", vaccine.Authority);
            vaccineObj.Add("ma", vaccine.VaccineManufacturer.Item1);
            vaccineObj.Add("mp", vaccine.Product.Item1); // ProductCode
            vaccineObj.Add("sd", vaccine.TotalSeriesOfDoses);
            vaccineObj.Add("tg", vaccine.DiseaseTargeted.Item1);
            vaccineObj.Add("vp", vaccine.VaccineType.Item1);
            //Administering centre is only part of the WHO interim standard, not the EU standard
            //https://www.who.int/publications/m/item/core-data-set-for-the-smart-vaccination-certificate
            //For now the key has been set to "ac", unclear what it should actually be. Has been commented out until certain what it should be
            //vaccineObj.Add("ac", vaccine.Site);

            return vaccineObj;
        }

        private CBORObject ToRecoveryCBOR(TestResultNhs diagnosticResult, DateTime? validityEndDate, string uniqueCertificateIdentifier)
        {
            // Please refer to https://github.com/ehn-digital-green-development/ehn-dgc-schema/blob/main/DGC.combined-schema.json
            // All fields key values are following official EU schema
            CBORObject recoveryObj = CBORObject.NewMap();
            recoveryObj.Add("tg", diagnosticResult.DiseaseTargeted.Item1); // disease-agent-targeted
            recoveryObj.Add("fr",
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(diagnosticResult.DateTimeOfTest, DateTimeKind.Unspecified), timeZoneInfo).ToString("yyyy-MM-dd"));
            // ISO 8601 Date of First Positive Test Result
            recoveryObj.Add("co", String.IsNullOrEmpty(diagnosticResult.CountryOfAuthority) ? configuration["RecoveryCountry"] : diagnosticResult.CountryOfAuthority); // Country of Test
            recoveryObj.Add("is", String.IsNullOrEmpty(diagnosticResult.Authority) ? configuration["RecoveryAuthority"] : diagnosticResult.Authority); // Certificate Issuer
            recoveryObj.Add("df", TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified), timeZoneInfo).ToString("yyyy-MM-dd")); // ISO 8601 Date: Certificate Valid From
            if(validityEndDate != null)
                recoveryObj.Add("du", TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(validityEndDate.Value, DateTimeKind.Unspecified), timeZoneInfo).ToString("yyyy-MM-dd")); // Certificate Valid Until
            recoveryObj.Add("ci", String.IsNullOrEmpty(uniqueCertificateIdentifier) ? String.Empty : uniqueCertificateIdentifier); // Unique Certificate Identifier, UVCI
            return recoveryObj;
        }

        private CBORObject ToTestResultCBOR(TestResultNhs testResult, string uniqueCertificateIdentifier)
        {
            CBORObject testResultObj = CBORObject.NewMap();
            testResultObj.Add("ci", String.IsNullOrEmpty(uniqueCertificateIdentifier) ? String.Empty : uniqueCertificateIdentifier); // Unique Certificate Identifier, UVCI            
            testResultObj.Add("co", testResult.CountryOfAuthority); // Country of Test
            testResultObj.Add("is", testResult.Authority); // Certificate Issuer
            testResultObj.Add("tt", testResult.TestType); //Test type
            //DateTime expressed as UTC time
            testResultObj.Add("sc", testResult.DateTimeOfTest.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")); //Date-time of test
            testResultObj.Add("tr", "260415000"); //Result of test
            testResultObj.Add("tg", testResult.DiseaseTargeted.Item1); // disease-agent-targeted
            if (!String.IsNullOrEmpty(testResult.RAT))
            {
                testResultObj.Add("ma", testResult.RAT);
            }
            testResultObj.Add("tc", testResult.TestLocation);
            if (!String.IsNullOrEmpty(testResult.TestKit))
            {
                testResultObj.Add("nm", testResult.TestKit);
            }
            return testResultObj;
        }

        private bool IsTestResult(TestResultNhs testResult)
        {
            return !string.IsNullOrEmpty(testResult.TestType);
        }

        private static CBORObject ToNameCBOR(IUserCBORInformation user)
        {
            if (string.IsNullOrEmpty(user.GivenName) || string.IsNullOrEmpty(user.FamilyName))
                return GetByParsingFullName(user);
            return GetFromFamilyAndGivenNames(user);
        }

        private static CBORObject GetFromFamilyAndGivenNames(IUserCBORInformation user)
        {
            var userNamesObj = CBORObject.NewMap();
            userNamesObj.Add("fn", user.FamilyName);
            userNamesObj.Add("gn", user.GivenName);

            var familyNameUpper = user.FamilyName?.ToUpper() ?? "";
            var givenNameUpper = user.GivenName?.ToUpper() ?? "";

            var transliterationResults = GetTransliterationResults(familyNameUpper, givenNameUpper);

            return GetNameRegex(userNamesObj, transliterationResults);
        }

        private static CBORObject GetNameRegex(CBORObject userNamesObj, List<string> transliterationResults)
        {
            var fnt = transliterationResults[0].Replace("-", "<").Replace(" ", "<").Replace("'", "<");
            var gnt = transliterationResults[1].Replace("-", "<").Replace(" ", "<").Replace("'", "<");

            var regex = new Regex(@"^[A-Z<]*$");
            if (regex.IsMatch(fnt) && regex.IsMatch(gnt))
            {
                userNamesObj.Add("fnt", fnt);
                userNamesObj.Add("gnt", gnt);
            }
            else
            {
                userNamesObj.Add("fnt", transliterationResults[0]);
                userNamesObj.Add("gnt", transliterationResults[1]);
            }
            return userNamesObj;
        }

        public static List<string> GetTransliterationResults(string familyNameUpper, string givenNameUpper)
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

        private static CBORObject GetByParsingFullName(IUserCBORInformation user)
        {
            var userNamesObj = CBORObject.NewMap();
            var fullName = user.Name.Split(' ');

            var firstAndMiddleNames = string.Join(' ', fullName.Take(fullName.Length - 1));
            var lastName = fullName.Last();
            var transliterationResults = GetTransliterationResults(lastName.ToUpper(), firstAndMiddleNames.ToUpper());

            userNamesObj.Add("fn", lastName.ToLower());
            userNamesObj.Add("gn", firstAndMiddleNames.ToLower());

            return GetNameRegex(userNamesObj, transliterationResults);
        }
    }
}
