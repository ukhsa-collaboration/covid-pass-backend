using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;
using CovidCertificate.Backend.Interfaces.TwoFactor;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using MongoDB.Driver;

namespace CovidCertificate.Backend.Auth
{
    public class GenerateOtpService
    {
        private readonly ILogger<GenerateOtpService> logger;
        private readonly IMongoRepository<OtpRequestDto> mongoRepoOtp;
        private readonly IConfiguration configuration;
        private readonly IFeatureManager featureManager;
        private readonly ISmsService smsService;
        private readonly IDateTimeProviderService dateTimeProviderService;

        public GenerateOtpService(ILogger<GenerateOtpService> logger,
            IMongoRepository<OtpRequestDto> mongoRepoOtp,
            IConfiguration configuration,
            IFeatureManager featureManager,
            ISmsService smsService,
            IDateTimeProviderService dateTimeProviderService)
        {
            this.logger = logger;
            this.mongoRepoOtp = mongoRepoOtp;
            this.configuration = configuration;
            this.featureManager = featureManager;
            this.smsService = smsService;
            this.dateTimeProviderService = dateTimeProviderService;
        }

        [FunctionName("GenerateOtp")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(NhsLoginTokenResponse), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "GenerateOtp")] HttpRequest request)
        {
            try
            {
                logger.LogInformation($"{nameof(GenerateOtpService)} was invoked");

                if (!request.Headers.TryGetValue("phoneNumber", out var phoneNumber))
                {
                    throw new ArgumentNullException(nameof(phoneNumber), "No phone number was supplied.");
                }

                var hashedPhoneNumber = StringUtils.GetHashString(phoneNumber);

                var existingOtps = (await mongoRepoOtp.FindAllAsync(x => x.PhoneNumberHash == hashedPhoneNumber)).ToList();

                foreach (var existingOtp in existingOtps.Where(x => x.IsStillValid == true || x.AttemptsLeft > 0))
                {
                    await InvalidateOtp(existingOtp);
                }

                if (existingOtps.Any(x => x.IsFinalGenerated == true))
                {
                    return new StatusCodeResult(429);
                }

                var maxOtpAttempts = configuration.GetValue<int>("MaxNumberOfOtpAttempts");
                var maxOtpGenerations = configuration.GetValue<int>("MaxNumberOfDailyOtpGenerationAttempts");

                var otpCode = StringUtils.RandomDigitCode(6);

                if (await featureManager.IsEnabledAsync(FeatureFlags.EnableOtpTesting))
                {
                    var testOtpCode = configuration.GetValue<string>("TestOtpCode");
                    otpCode = testOtpCode;
                }

                var isFinalGenerated = existingOtps.Count() >= maxOtpGenerations - 1;
           
                var otpDto = new OtpRequestDto(hashedPhoneNumber, otpCode,
                    maxOtpAttempts, isFinalGenerated, isStillValid: true, createdAt: dateTimeProviderService.UtcNow);

                await mongoRepoOtp.InsertOneAsync(otpDto);

                if (!await featureManager.IsEnabledAsync(FeatureFlags.EnableOtpTesting))
                {
                    Dictionary<string, dynamic> personalisation = new Dictionary<string, dynamic>
                    {
                        {"OTP", otpCode}
                    };

                    await smsService.SendSmsAsync(personalisation, phoneNumber, configuration["OtpSmsTemplateId"]);
                }

                logger.LogInformation($"{nameof(GenerateOtpService)} has finished");

                return new OkResult();
            }
            catch(Notify.Exceptions.NotifyClientException e)
            {
                logger.LogWarning(e, e.Message);
                return new BadRequestObjectResult(e.Message);
            }
            catch (Exception e) when (e is BadRequestException || e is ArgumentNullException)
            {
                logger.LogWarning(e, e.Message);
                return new BadRequestObjectResult("There seems to be a problem: bad request");
            }
            catch (UnauthorizedException e)
            {
                logger.LogWarning(e, e.Message);
                return new UnauthorizedObjectResult("There seems to be a problem: unauthorized");
            }
            catch (HttpRequestException e)
            {
                logger.LogCritical(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task InvalidateOtp(OtpRequestDto existingOtp)
        {
            var invalidateUsedOtp = Builders<OtpRequestDto>.Update
                .Set(x => x.AttemptsLeft, 0)
                .Set(x => x.IsStillValid, false);

            await mongoRepoOtp.UpdateOneAsync(invalidateUsedOtp, x => x.Id == existingOtp.Id);
        }
    }
}
