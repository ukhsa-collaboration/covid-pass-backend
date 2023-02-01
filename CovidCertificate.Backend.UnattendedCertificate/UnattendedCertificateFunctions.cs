using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CovidCertificate.Backend.Models.Deserializers;
using Hl7.Fhir.Model;
using CovidCertificate.Backend.Models.DataModels;
using System.Net;
using CovidCertificate.Backend.Utils;
using System.Globalization;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Utils.Extensions;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.UnattendedCertificate.ErrorHandling;
using CovidCertificate.Backend.UnattendedCertificate.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using CovidCertificate.Backend.Interfaces;
using Newtonsoft.Json;
using CovidCertificate.Backend.UnattendedCertificate.Models;
using CovidCertificate.Backend.Models.RequestDtos;
using System.Text.RegularExpressions;

namespace CovidCertificate.Backend.UnattendedCertificate
{
    public class UnattendedCertificateFunctions
    {
        private const string DomesticApiName = "CreateUnattendedDomesticCertificate";
        private const string VaccinationApiName = "CreateUnattendedVaccinationCertificate";
        private const string RecoveryApiName = "CreateUnattendedRecoveryCertificate";
        private const string PDFEmailIngestionName = "CreateUnattendedPDF";

        private static readonly UnattendedFhirPatientValidator unattendedFhirPatientValidator = new UnattendedFhirPatientValidator();

        private readonly ICovidCertificateService covidCertificateService;
        private readonly IDomesticCertificateWrapper domesticCertificateWrapper;
        private readonly IInternationalCertificateWrapper internationalCertificateWrapper;
        private readonly ILogger<UnattendedCertificateFunctions> logger;
        private readonly IFeatureManager featureManager;
        private readonly IPdfContentGenerator pdfContentGenerator;
        private readonly IQueueService queueService;
        private readonly IConfiguration configuration;
        private readonly ICovidResultsService covidResultsService;

        public UnattendedCertificateFunctions(ILogger<UnattendedCertificateFunctions> logger,
            ICovidCertificateService covidCertificateService,
            IDomesticCertificateWrapper domesticCertificateWrapper,
            IInternationalCertificateWrapper internationalCertificateWrapper,
            IFeatureManager featureManager,
            IPdfContentGenerator pdfContentGenerator,
            IQueueService queueService,
            IConfiguration configuration,
            ICovidResultsService covidResultsService)
        {
            this.logger = logger;
            this.covidCertificateService = covidCertificateService;
            this.domesticCertificateWrapper = domesticCertificateWrapper;
            this.internationalCertificateWrapper = internationalCertificateWrapper;
            this.featureManager = featureManager;
            this.pdfContentGenerator = pdfContentGenerator;
            this.queueService = queueService;
            this.configuration = configuration;
            this.covidResultsService = covidResultsService;
        }

        [FunctionName(DomesticApiName)]
        public async Task<IActionResult> PostUnattendedDomesticCertificateAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "certificate/domestic")] HttpRequest req)
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.EnableDomestic))
            {
                return new BadRequestObjectResult(new Error { ErrorCode = ((ushort)ErrorCode.DISABLED_ENDPOINT).ToString() });
            }
            return await CreateUnattendedCertificateAsync(req, CertificateScenario.Domestic);
        }

        [FunctionName(VaccinationApiName)]
        public async Task<IActionResult> PostUnattendedVaccinationCertificateAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "certificate/vaccination")] HttpRequest req)
        {
            var allowPrimaryDoseCertificates = req.Query["allowPrimaryDoseCertificates"].ToString();

            if (string.IsNullOrEmpty(allowPrimaryDoseCertificates))
            {
                return await CreateUnattendedCertificateAsync(req, CertificateScenario.International);
            }

            if (!bool.TryParse(allowPrimaryDoseCertificates, out var value))
            {
                logger.LogInformation("Provided header 'allowPrimaryDoseCertificates' was not in correct format");

                return new BadRequestObjectResult(new Error { ErrorCode = ((ushort)ErrorCode.ILLEGAL_VALUE_PASSED).ToString() });
            }

            return await CreateUnattendedCertificateAsync(req, CertificateScenario.International, value);
        }
        [FunctionName(PDFEmailIngestionName)]
        public async System.Threading.Tasks.Task GetInternationalPDF(
               [ServiceBusTrigger("%UnattendedPDFRequests%",
                Connection = "ServiceBusConnectionString")] UnattendedPdfRequest myQueueItem)
        {
            var user = CreateUserFromPatient(FHIRDeserializer.Deserialize<Patient>(myQueueItem.FHIRPatient));
            
            try
            {
                var medicalRecords = await covidResultsService.GetMedicalResultsAsync(user, "", CertificateScenario.International, NhsdApiKey.Unattended);
                var certificateContainerVaccine = await covidCertificateService.GetInternationalUnattendedCertificateAsync(user, CertificateType.Vaccination, medicalRecords);
                var vaccineCert = certificateContainerVaccine.GetSingleCertificateOrNull();
                var certificateContainerRecovery = await covidCertificateService.GetInternationalUnattendedCertificateAsync(user, CertificateType.Recovery, medicalRecords);
                var recoveryCert = certificateContainerRecovery.GetSingleCertificateOrNull();
                
                if (vaccineCert == null && recoveryCert == null)
                {
                    throw new NoResultsException("Failure to generate pdf, failure message sent");
                }
                
                logger.LogInformation("Certificate(s) found, generating pdf content");
                
                var pdfContent = await pdfContentGenerator.GenerateInternationalAsync(
                    covidPassportUser: user,
                    vaccinationCertificate: vaccineCert,
                    recoveryCertificate: recoveryCert,
                    languageCode: "en",
                    type: PDFType.VaccineAndRecovery,
                    doseNumber: -1
                ); //dosenumber -1 will print all vaccines

                logger.LogInformation("PDF content generated");
                
                var emailContent = new PdfGenerationRequestInternationalDto
                {
                    Email = myQueueItem.EmailToSendTo,
                    PdfContent = pdfContent,
                    Name = user.Name,
                    LanguageCode = "en"
                };
                
                await queueService.SendMessageAsync("send_certificate_request_int", emailContent);
                
                logger.LogInformation("Email has finished");
            }
            catch (Exception e) when (e is NoResultsException || e is NoUnattendedVaccinesFoundException)
            {
                logger.LogInformation("Caught exception- generating failure message");
                var req = new LetterRequest
                {
                    CorrelationId = myQueueItem.CorrelationId,
                    NhsNumber = user.NhsNumber,
                    FirstName = user.GivenName,
                    LastName = user.FamilyName,
                    MobileNumber = myQueueItem.MobileNumber,
                    EmailAddress = myQueueItem.EmailToSendTo,
                    DateOfBirth = user.DateOfBirth.ToString(),
                    ContactMethod = (ContactMethodEnum)myQueueItem.ContactMethodSettable
                };
                var failureNotification = new FailureNotification
                {
                    ReasonCode = NotificationReasonCode.MissingRecord,
                    Request = req
                };

                var failMessage = JsonConvert.SerializeObject(failureNotification);
                logger.LogInformation("Failure message generated, adding to failure queue");
                await queueService.SendMessageAsync("pdffailure", failMessage);
                logger.LogInformation("Failure message sent, method finished");
                return;
            }
            catch (Exception e)
            {
                logger.LogCritical(e, e.Message);
                return;
            }
        }

        [FunctionName(RecoveryApiName)]
        public async Task<IActionResult> PostUnattendedRecoveryCertificateAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "certificate/recovery")] HttpRequest req)
        {
            using StreamReader streamReader = new StreamReader(req.Body);
            var rawRequestBody = await streamReader.ReadToEndAsync();

            try
            {
                var patient = FHIRDeserializer.Deserialize<Patient>(rawRequestBody);


                var badRequestResult = ValidatePatient(patient, requiredAge: configuration.GetValue<int>("UnattendedTestRequiredAge"));

                if (badRequestResult != null)
                {
                    return badRequestResult;
                }

                if (PatientHasInvalidCharsInName(patient))
                {
                    return new BadRequestObjectResult("Patient Name Contains invalid characters");
                }

                var user = CreateUserFromPatient(patient);

                var medicalRecords = await covidResultsService.GetMedicalResultsAsync(user, "", CertificateScenario.International, NhsdApiKey.Unattended, CertificateType.Recovery);
                var certificateContainer = await covidCertificateService.GetInternationalUnattendedCertificateAsync(user, CertificateType.Recovery, medicalRecords);
                var certificate = certificateContainer.GetSingleCertificateOrNull();

                if (certificate == default)
                {
                    logger.LogTraceAndDebug($"No recovery data found.");
                    logger.LogInformation($"{RecoveryApiName} has finished");
                    return new OkObjectResult(new Error() { ErrorCode = ((ushort)ErrorCode.NO_TEST_RESULTS_GRANTING_RECOVERY_FOUND).ToString() });
                }

                var result = internationalCertificateWrapper.WrapRecovery(certificateContainer);

                return new OkObjectResult(result);
            }
            catch (FormatException ex)
            {
                logger.LogError(ex, "Cannot parse FHIR payload.");

                return new BadRequestObjectResult(new Error { ErrorCode = ((ushort)ErrorCode.FHIR_PATIENT_INVALID).ToString() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);

                return new ObjectResult(new Error() { ErrorCode = ((ushort)ErrorCode.UNKNOWN_ERROR).ToString() })
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        private bool PatientHasInvalidCharsInName(Patient patient)
        {
            string regex = @"([&*()_=+""£$¬`|/@:;,<>[\]#!?~)])|([-']{2})";
            var foreName = patient.Name.FirstOrDefault().Given.FirstOrDefault().ToString();
            var familyName = patient.Name.FirstOrDefault().Family.ToString();
            var invalidCharsForename = Regex.Match(foreName, regex);
            var invalidCharsFamilyName = Regex.Match(familyName, regex);

            if (invalidCharsFamilyName.Length > 0 || invalidCharsForename.Length > 0)
            {
                return true;
            }
            return false;
        }

        private async Task<IActionResult> CreateUnattendedCertificateAsync(HttpRequest req, CertificateScenario scenario, bool allowPrimaryDoseCertificates = false)
        {
            using StreamReader streamReader = new StreamReader(req.Body);
            string rawRequestBody = await streamReader.ReadToEndAsync();
            try
            {
                var patient = FHIRDeserializer.Deserialize<Patient>(rawRequestBody);

                var badRequestResult = scenario switch
                {
                    CertificateScenario.Domestic => ValidatePatient(patient, requiredAge: configuration.GetValue<int>("UnattendedDomesticRequiredAge")),
                    CertificateScenario.International => ValidatePatient(patient, requiredAge: configuration.GetValue<int>("UnattendedVaccinationRequiredAge")),
                    CertificateScenario.Isolation => throw new Exception("Invalid: Should never be run "),
                };

                if (badRequestResult != null)
                {
                    return badRequestResult;
                }

                if (PatientHasInvalidCharsInName(patient))
                {
                    return new BadRequestObjectResult("Patient Name Contains invalid characters");
                }

                var user = CreateUserFromPatient(patient);

                return scenario switch
                {
                    CertificateScenario.Domestic => await CreateDomesticResponseObjectAsync(user),
                    CertificateScenario.International => await CreateInternationalResponseObjectAsync(user, allowPrimaryDoseCertificates),
                    CertificateScenario.Isolation => throw new Exception("Invalid: Should never be run "),
                };
            }
            catch (NoUnattendedVaccinesFoundException ex)
            {
                logger.LogInformation(ex, "No vaccines found when trying to create vaccination-based certificate in unattended mode");
                var errorCode = scenario switch
                {
                    CertificateScenario.Domestic => ((ushort)ErrorCode.NO_RECORDS_FOUND).ToString(),
                    CertificateScenario.International => ((ushort)ErrorCode.NO_VACCINES_FOUND).ToString(),
                    CertificateScenario.Isolation => throw new Exception("Invalid: Should never be run "),
                };

                return new OkObjectResult(new Error() { ErrorCode = errorCode });
            }
            catch (FormatException ex)
            {
                logger.LogError(ex, "Cannot parse FHIR payload.");

                return new BadRequestObjectResult(new Error { ErrorCode = ((ushort)ErrorCode.FHIR_PATIENT_INVALID).ToString() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);

                return new ObjectResult(new Error() { ErrorCode = ((ushort)ErrorCode.UNKNOWN_ERROR).ToString() })
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        private BadRequestObjectResult ValidatePatient(Patient patient, int requiredAge)
        {
            var validationResult = unattendedFhirPatientValidator.Validate(patient);

            BadRequestObjectResult badRequestResult = default;

            if (!validationResult.IsValid)
            {
                badRequestResult = new BadRequestObjectResult(
                    new Error()
                    {
                        ErrorCode = validationResult.Errors.FirstOrDefault().ErrorCode
                    });
                return badRequestResult;
            }

            if (IsUserUnderage(patient, requiredAge))
            {
                badRequestResult = new BadRequestObjectResult(
                    new Error()
                    {
                        ErrorCode = ((ushort)ErrorCode.FHIR_PATIENT_UNDERAGE).ToString()
                    });
            }

            //if no validation errors are present, then null is returned
            return badRequestResult;
        }

        private bool IsUserUnderage(Patient patient, int requiredAge)
        {
            var date = DateTime.ParseExact(patient.BirthDate, DateUtils.FHIRDateFormat, null, DateTimeStyles.None);
            return requiredAge > DateUtils.GetAgeInYears(date);
        }

        private static CovidPassportUser CreateUserFromPatient(Patient patient)
        {
            var date = DateTime.ParseExact(patient.BirthDate, DateUtils.FHIRDateFormat, null, DateTimeStyles.None);

            return new CovidPassportUser(
                $"{patient.Name.First().Given.First()} {patient.Name.First().Family}",
                date,
                null,
                null,
                patient.Identifier.First().Value,
                patient.Name.First().Family,
                patient.Name.First().Given.First(),
                IdentityProofingLevel.P9
            );
        }

        private async Task<IActionResult> CreateInternationalResponseObjectAsync(CovidPassportUser user, bool allowPrimaryDoseCertificates)
        {
            var medicalResults = await covidResultsService.GetMedicalResultsAsync(user, "", CertificateScenario.International, NhsdApiKey.Unattended, CertificateType.Vaccination);
            var certificateContainer = await covidCertificateService.GetInternationalUnattendedCertificateAsync(user, CertificateType.Vaccination, medicalResults);

            var cert = certificateContainer.GetSingleCertificateOrNull();

            if (cert == null)
            {
                return new OkObjectResult(new Error() { ErrorCode = ((ushort)ErrorCode.INVALID_VACCINES).ToString() });
            }

            if (!AllowCertificateGeneration(allowPrimaryDoseCertificates, cert))
            {
                return new OkObjectResult(new Error() { ErrorCode = ((ushort)ErrorCode.INVALID_VACCINES).ToString() });
            }
            var result = internationalCertificateWrapper.WrapVaccines(certificateContainer);

            return new OkObjectResult(result);
        }

        private static bool AllowCertificateGeneration(bool allowPrimaryDoseCertificates, Certificate cert)
        {
            if (allowPrimaryDoseCertificates)
            {
                //if we reached this place then certificate is generated and at least one vaccine is present
                return true;
            }

            var results = cert.GetAllVaccinationsFromEligibleResults();
            if (!results.Any(x => !x.IsBooster))
            {
                //if certificate has no full primary course vaccination then cert is not valid
                return false;
            }

            //check if the required number of doses is met
            return results.First().TotalSeriesOfDoses <= results.Count();
        }

        private async Task<IActionResult> CreateDomesticResponseObjectAsync(CovidPassportUser user)
        {
            var medicalResults = await covidResultsService.GetMedicalResultsAsync(user, "", CertificateScenario.Domestic, NhsdApiKey.Unattended);
            var certificateContainer = await covidCertificateService.GetDomesticUnattendedCertificateAsync(user, medicalResults);

            var cert = certificateContainer.GetSingleCertificateOrNull();

            if (cert == null)
            {
                return new OkObjectResult(new Error() { ErrorCode = ((ushort)ErrorCode.EVENT_HISTORY_NOT_MET).ToString() });
            }

            var expiredCertificateExists = await covidCertificateService.ExpiredCertificateExistsAsync(user, certificateContainer.GetSingleCertificateOrNull());

            var result = domesticCertificateWrapper.WrapAsync(certificateContainer, expiredCertificateExists);

            return new OkObjectResult(result);
        }
    }
}
