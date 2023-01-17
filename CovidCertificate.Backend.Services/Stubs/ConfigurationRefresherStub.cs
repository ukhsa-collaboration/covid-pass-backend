using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.Stubs
{
    // Please note that this class is supposed to be used only for local development purposes
    public class ConfigurationRefresherStub : IConfigurationRefresher
    {
        public void ProcessPushNotification(PushNotification pushNotification, TimeSpan? maxDelay = null)
        {
            throw new NotImplementedException();
        }

        public Uri AppConfigurationEndpoint => throw new NotImplementedException();
        public ILoggerFactory LoggerFactory { get; set; }

        public Task RefreshAsync()
        {
            return Task.CompletedTask;
        }

        public Task RefreshAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        public Task<bool> TryRefreshAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(true);
        }

        public void SetDirty(TimeSpan? maxDelay = null)
        {
            return;
        }

        public Task<bool> TryRefreshAsync()
        {
            return Task.FromResult(true);
        }
    }
}
