using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using CovidCertificate.Backend.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Services.AzureServices
{
    public class ServiceBusQueueService : IQueueService
    {
        private readonly ILogger<ServiceBusQueueService> logger;
        private readonly string ServiceBusConnection;
        private readonly ServiceBusClient client;
        private readonly Dictionary<string, ServiceBusSender> senders = new Dictionary<string, ServiceBusSender>();

        public ServiceBusQueueService(IConfiguration configuration, ILogger<ServiceBusQueueService> logger)
        {
            this.ServiceBusConnection = configuration["ServiceBusConnectionString"];
            if (ServiceBusConnection == default)
                throw new ApplicationException("Service Bus connection is not setup");

            client = new ServiceBusClient(ServiceBusConnection);
            this.logger = logger;
        }

        private ServiceBusSender GetServiceBusSender(string queueName)
        {
            if (!senders.ContainsKey(queueName))
            {
                senders[queueName] = client.CreateSender(queueName);
            }

            return senders[queueName];
        }

        private static Queue<ServiceBusMessage> CreateMessages(IEnumerable<string> messages)
        {
            var sbMessages = new Queue<ServiceBusMessage>();
            foreach (var message in messages)
            {
                sbMessages.Enqueue(new ServiceBusMessage(message));
            }

            return sbMessages;
        }

        private async Task<bool> SendMessageAsync(string queueName, string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            try
            {
                var sender = GetServiceBusSender(queueName);
                var queueMessage = new ServiceBusMessage(message);

                // send the message
                await sender.SendMessageAsync(queueMessage);

                return true;
            }
            catch (Exception e)
            {
                logger.LogCritical(e, e.Message);
                throw;
            }
        }

        public async Task<bool> SendMessageAsync<T>(string queueName, T messageObject) where T : class
        {
            var message = JsonConvert.SerializeObject(messageObject);
            return await SendMessageAsync(queueName, message);
        }
    }
}
