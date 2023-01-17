using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
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
    public class VerifyOtpService
    {
        private readonly ILogger<VerifyOtpService> logger;
        private readonly IMongoRepository<OtpRequestDto> mongoRepoOtp;
        private readonly IFeatureManager featureManager;
        private readonly IConfiguration configuration;

        public VerifyOtpService(ILogger<VerifyOtpService> logger, IMongoRepository<OtpRequestDto> mongoRepoOtp, IFeatureManager featureManager, IConfiguration configuration)
        {
            this.logger = logger;
            this.mongoRepoOtp = mongoRepoOtp;
            this.featureManager = featureManager;
            this.configuration = configuration;
        }

        [FunctionName("VerifyOtp")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(NhsLoginTokenResponse), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "VerifyOtp")] HttpRequest request)
        {
            try
            {
                logger.LogInformation($"{nameof(VerifyOtpService)} was invoked");

                if (!request.Headers.TryGetValue("phoneNumber", out var phoneNumber))
                {
                    throw new ArgumentNullException(nameof(phoneNumber), "No phone number was supplied.");
                }

                if (!request.Headers.TryGetValue("otpCode", out var otpCode))
                {
                    throw new ArgumentNullException(nameof(otpCode), "No OTP code was supplied.");
                }

                var hashedPhoneNumber = StringUtils.GetHashString(phoneNumber);



                var existingOtpList = await mongoRepoOtp.FindAllAsync(x => x.PhoneNumberHash == hashedPhoneNumber);
                var sortedExistingOtps = existingOtpList.OrderByDescending(x => x.CreatedAt);
                var otpTimeToLive = configuration.GetValue<int>("OtpTimeToLive");

                foreach (var existingOtp in sortedExistingOtps)
                {
                    if (existingOtp.OtpCode == otpCode)
                    {
                        var createdAt = existingOtp.CreatedAt ?? DateTime.UtcNow;

                        var isInvalid = existingOtp.IsStillValid == false || createdAt.AddMinutes(otpTimeToLive) <= DateTime.UtcNow;
                        await InvalidateOtp(existingOtp);

                        logger.LogInformation($"{nameof(VerifyOtpService)} has finished");

                        if (isInvalid)
                        {
                            return new StatusCodeResult(StatusCodes.Status410Gone);
                        }

                        return new OkObjectResult("OTP code verified succesfully");
                    }

                    if (existingOtp.IsStillValid == true || existingOtp.AttemptsLeft > 0)
                    {
                        var attemptsLeftDecremented = existingOtp.AttemptsLeft - 1;

                        if (attemptsLeftDecremented <= 0)
                        {
                            await InvalidateOtp(existingOtp);
                        }
                        else
                        {
                            await DecrementOtpAttempts(existingOtp, attemptsLeftDecremented);
                        }

                        logger.LogInformation($"{nameof(VerifyOtpService)} has finished");
                        return new BadRequestObjectResult(attemptsLeftDecremented);
                    }

                }


                logger.LogInformation($"{nameof(VerifyOtpService)} has finished");

                return new BadRequestObjectResult(0);

            }
            catch (Exception e) when (e is BadRequestException || e is ArgumentNullException)
            {
                logger.LogWarning(e, e.Message);
                return new BadRequestObjectResult(0);
            }
            catch (UnauthorizedException e)
            {
                logger.LogWarning(e, e.Message);
                return new UnauthorizedObjectResult(0);
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

        private async Task DecrementOtpAttempts(OtpRequestDto existingOtp, int remainingAttempts)
        {
            var invalidateUsedOtp = Builders<OtpRequestDto>.Update
                .Set(x => x.AttemptsLeft, remainingAttempts);

            await mongoRepoOtp.UpdateOneAsync(invalidateUsedOtp, x => x.Id == existingOtp.Id);
        }
    }
}
