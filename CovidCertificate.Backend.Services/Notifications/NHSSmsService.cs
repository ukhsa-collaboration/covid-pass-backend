using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.TwoFactor;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notify.Client;
using Notify.Models.Responses;

namespace CovidCertificate.Backend.Services.Notifications
{
    public class NHSSmsService : ISmsService
    {
        private readonly NotificationClient client;
        private readonly ILogger<NHSSmsService> logger;

        public NHSSmsService(ILogger<NHSSmsService> logger, IConfiguration configuration)
        {
            this.logger = logger;
            client = new NotificationClient(configuration["NHSNotifcationAPIKey"]);
        }

        public async Task SendSmsAsync(Dictionary<string, dynamic> personalisation, string phoneNumber, string templateId)
        {
            logger.LogInformation($"{nameof(SendSmsAsync)} was invoked");

            var response = await client.SendSmsAsync(
                mobileNumber: phoneNumber,
                templateId: templateId, personalisation);

            logger.LogInformation($"{nameof(SendSmsAsync)} response: {response}");

            logger.LogInformation($"{nameof(SendSmsAsync)} has finished");
        }
    }
}
