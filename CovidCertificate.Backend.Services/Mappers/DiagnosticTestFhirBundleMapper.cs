using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Exceptions;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.Mappers
{
    public class DiagnosticTestFhirBundleMapper : IFhirBundleMapper<TestResultNhs>
    {
        private const string BlobContainerNameConfigKey = "BlobContainerNameTestMappings";
        private const string BlobFileNameConfigKey = "BlobFileNameTestMappings";

        private readonly IBlobFilesInMemoryCache<TestMappings> _mappings;
        private readonly ILogger<DiagnosticTestFhirBundleMapper> logger;
        private readonly IConfiguration configuration;

        private string BlobContainerName => this.configuration.GetValue<string>(BlobContainerNameConfigKey);
        private string BlobFileName => this.configuration.GetValue<string>(BlobFileNameConfigKey);

        public DiagnosticTestFhirBundleMapper(IBlobFilesInMemoryCache<TestMappings> mappings, IConfiguration configuration, ILogger<DiagnosticTestFhirBundleMapper> logger)
        { 
            this._mappings = mappings;
            this.logger = logger;
            this.configuration = configuration;
        }

        public async Task<IEnumerable<TestResultNhs>> ConvertBundleAsync(Bundle bundle)
        {
            var testResults = new List<TestResultNhs>();

            if (bundle != null)
            {
                var mappings = await _mappings.GetFileAsync(
                    BlobContainerName, BlobFileName);

                foreach (var entry in bundle.Entry)
                {
                    try
                    {
                        if (entry?.Resource is Observation observation)
                        {
                            testResults.Add(ConvertObservation(observation, mappings, null, null));
                        }
                    }
                    catch(DiagnosticTestMappingException e)
                    {
                        logger.LogError(e, e.Message);
                    }
                }
            }

            return testResults;
        }

        public async Task<TestResultNhs> ConvertObservationWithDeviceAsync(Observation observation, Device device, Organization organization, string countryOfAuthority, string issuer)
        {
            var mappings = await _mappings.GetFileAsync(
                configuration.GetValue<string>(BlobContainerNameConfigKey),
                configuration.GetValue<string>(BlobFileNameConfigKey));
            TestResultNhs testResult = ConvertObservation(observation, mappings, countryOfAuthority, issuer);
            testResult.TestKit = null;
            if (device != null)
            {
                if (device.Manufacturer != null && device.DeviceName != null && device.DeviceName.Any())
                {
                    testResult.TestKit = device.Manufacturer + ", " + device.DeviceName.FirstOrDefault().Name;
                }
                testResult.TestType = device.Identifier.Single(x => "http://loinc.org".Equals(x.System)).Value;

                IEnumerable<Identifier> ratIdentifiers = device.Identifier.Where(x =>
                    "https://covid-19-diagnostics.jrc.ec.europa.eu/devices/hsc-common-recognition-rat".Equals(x.System));
                if (ratIdentifiers.Any())
                {
                    testResult.RAT = ratIdentifiers.FirstOrDefault().Value;
                }
            }
            if (organization != null)
            {
                testResult.TestLocation = organization.Name;
            }

            return testResult;
        }

        public async Task<TestResultNhs> ConvertObservationAsync(Observation observation, string countryOfAuthority, string issuer)
        {
            var mappings = await _mappings.GetFileAsync(
                configuration.GetValue<string>(BlobContainerNameConfigKey),
                configuration.GetValue<string>(BlobFileNameConfigKey));
            return ConvertObservation(observation, mappings, countryOfAuthority, issuer);
        }

        private TestResultNhs ConvertObservation(Observation observation, TestMappings mappings, string countryOfAuthority, string authority)
        {            

            (string testKit, string validityType) = GetTestKitAndValidityType(observation.Device?.Identifier, mappings);

            var fhirDateTimeOfTest = (FhirDateTime)observation.Effective;
            var dateTimeOfTestNullable = fhirDateTimeOfTest?.ToDateTimeOffset(TimeZoneInfo.Utc.GetUtcOffset(DateTime.Now)).DateTime;
            if (!dateTimeOfTestNullable.HasValue)
            {
                throw new DiagnosticTestMappingException($"{nameof(DiagnosticTestFhirBundleMapper)}: DateTimeOfTest needs to have a value.");
            }
            var dateTimeOfTest = dateTimeOfTestNullable.Value;

            var codeableConcept = (CodeableConcept)observation.Value;
            var codeableConceptCoding = codeableConcept.Coding?.FirstOrDefault()?.Code;
            if (codeableConceptCoding == null)
            {
                throw new DiagnosticTestMappingException($"{nameof(DiagnosticTestFhirBundleMapper)}: CodeableConcept code is null.");
            }
            var resultMapping = mappings.Result;
            var result = resultMapping.TryGetValue(codeableConceptCoding, out var b) ? b : null;
            if (result == null)
            {
                throw new DiagnosticTestMappingException($"{nameof(DiagnosticTestFhirBundleMapper)}: CodeableConcept code with SNOMED value {codeableConceptCoding} does not exist in mapping file.");
            }

            var selfTestMapping = mappings.SelfTestValues;

            var processingCode = observation.Performer != null ? GetProcessingCode(validityType, observation.Performer, selfTestMapping) : "LAB_RESULT";

            var staticValuesMapping = mappings.StaticValues;

            var diseaseTargetedCode = staticValuesMapping.TryGetValue("DiseaseTargetedCode", out var c) ? c : string.Empty;
            var diseaseTargetedValue = staticValuesMapping.TryGetValue("DiseaseTargetedValue", out var d) ? d : string.Empty;
            if (countryOfAuthority == null)
            {
                countryOfAuthority = staticValuesMapping.TryGetValue("Country", out var e) ? e : string.Empty;
            }

            if (authority == null)
            {
                authority = staticValuesMapping.TryGetValue("Authority", out var f) ? f : string.Empty;
            }
            var isNAAT = mappings.IsNAAT.Contains(validityType, StringComparer.OrdinalIgnoreCase);

            return new TestResultNhs(dateTimeOfTest, result, validityType, processingCode, testKit, new Tuple<string, string>(diseaseTargetedCode, diseaseTargetedValue), authority, countryOfAuthority, isNAAT);
        }

        private Tuple<string, string> GetTestKitAndValidityType(Identifier identifier, TestMappings mappings)
        {
            String testKit;
            String validityType;

            if (identifier == null)
            {
                validityType = "UNKNOWN";
                testKit = null;
                logger.LogInformation($"{nameof(DiagnosticTestFhirBundleMapper)}: observation's device identifier null. Setting validityType to UNKNOWN");
            }
            else
            {
                //The identifer value could be intentionally empty and map to a value as per documentation
                testKit = identifier.Value?.ToUpper() ?? string.Empty;
                var testKitMapping = mappings.Type;

                if (testKitMapping.TryGetValue(testKit.ToUpperInvariant(), out var a))
                {
                    validityType = a;
                }
                else
                {
                    validityType = "UNKNOWN";
                    //Log warning as all known Test Kit values should be in mapping file
                    logger.LogWarning($"{nameof(DiagnosticTestFhirBundleMapper)}: Test Kit value {testKit} does not exist in mapping file. Mapping to UNKNOWN.");
                }
            }
            return new Tuple<string, string>(testKit, validityType);
        }

        private string GetProcessingCode(string validityType, IEnumerable<ResourceReference> performers, IDictionary<string, string> mappings)
        {
            foreach(var performer in performers)
            {
                var performerValue = performer.Identifier?.Value;
                if(performerValue != null)
                {
                    var selfTestType = mappings.TryGetValue(performerValue.ToUpper(), out var a) ? a : string.Empty;
                    if (string.IsNullOrEmpty(selfTestType))
                    {
                        logger.LogWarning($"{nameof(DiagnosticTestFhirBundleMapper)}: Performer value {performerValue} does not exist in self test mapping file.");
                    }
                    if (validityType == selfTestType &&
                       ((validityType == "PCR" && performer.Identifier?.Type?.Text?.Equals("Testing Centre", StringComparison.InvariantCultureIgnoreCase) == true) || validityType == "LFT"))
                    {
                        return "SelfTest";
                    }
                }                          
            }

            if(validityType == "LFT")
            {
                return "Assist";
            }

            return "LAB_RESULT";
        }
    }
}
