using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Auth
{
    public static class WarmUpAuthFunction
    {
        [FunctionName("WarmUpAuthFunction")]
        public static void Run([TimerTrigger("0 */4 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");
        }
    }
}
