using System;
using System.Net.Http;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.TwoFactor;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Notify.Client;
using Notify.Exceptions;

namespace CovidCertificate.Backend.Services.Notifications
{
    public class NHSEmailService : IEmailService
    {
        private readonly NotificationClient client;
        private readonly ILogger<NHSEmailService> logger;
        private readonly IFeatureManager featureManager;

        public NHSEmailService(
            ILogger<NHSEmailService> logger, 
            IConfiguration configuration, 
            IFeatureManager featureManager,
            IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            var httpClientWrapper = new HttpClientWrapper(httpClientFactory.CreateClient());
            client = new NotificationClient(httpClientWrapper, configuration["NHSNotifcationAPIKey"]);
            this.featureManager = featureManager;
        }

        public async Task SendEmailAsync<T>(T emailContent, string templateId) where T : IEmailModel
        {
            logger.LogInformation("SendEmailAsync was invoked");
            if (!await featureManager.IsEnabledAsync(FeatureFlags.Notify))
            {
                logger.LogInformation("SendEmailAsync has finished");
                return;
            }

            try
            {
                var personalisation = emailContent.GetPersonalisation();
                var response = await client.SendEmailAsync(emailContent.EmailAddress, templateId, personalisation);
                logger.LogInformation("SendEmail has finished");
            }
            catch (NotifyClientException e)
            {
                logger.LogTraceAndDebug($"Not able to send email. Received error: {e.Message}");
                throw;
            }
            catch (Exception e)
            {
                logger.LogTraceAndDebug($"Failed to send email. Received error: {e.Message}, {e.StackTrace}");
                throw;
            }
        }

        public async Task SendPdfSizeFailureEmailAsync(string emailAddress, string govUkNotifyTemplateGuid)
        {
            logger.LogInformation("SendPdfSizeFailureEmailAsync was invoked");
            
            if (!await featureManager.IsEnabledAsync(FeatureFlags.Notify))
            {
                logger.LogInformation("SendPdfSizeFailureEmailAsync has finished");
                return;
            }

            try
            {
                var response = await client.SendEmailAsync(emailAddress, govUkNotifyTemplateGuid);
                logger.LogInformation("SendEmail has finished");
            }
            catch (NotifyClientException e)
            {
                logger.LogTraceAndDebug($"Not able to send email. Received error: {e.Message}");
                throw;
            }
            catch (Exception e)
            {
                logger.LogTraceAndDebug($"Failed to send email. Received error: {e.Message}, {e.StackTrace}");
                throw;
            }
        }
    }
}
