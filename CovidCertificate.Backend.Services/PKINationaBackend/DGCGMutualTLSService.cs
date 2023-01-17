using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.PKINationalBackend.Models;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using Polly.Wrap;

namespace CovidCertificate.Backend.PKINationalBackend.Services
{
    [ExcludeFromCodeCoverage]
    public class DGCGMutualTLSService : IDGCGMutualTLSService
    {
        private readonly ILogger<DGCGMutualTLSService> logger;
        private readonly DGCGSettings settings;
        private AsyncPolicyWrap<HttpResponseMessage> retryPolicy;
        private readonly ICertificateCache certificateCache;

        public DGCGMutualTLSService(ILogger<DGCGMutualTLSService> logger, DGCGSettings settings, ICertificateCache certificateCache)
        {
            this.logger = logger;
            this.settings = settings;
            SetupRetryPolicy();
            this.certificateCache = certificateCache;
        }

        public async Task<string> MakeMutuallyAuthenticatedRequestAsync(string endpoint)
        {
            logger.LogTraceAndDebug($"{nameof(DGCGMutualTLSService)}: {nameof(MakeMutuallyAuthenticatedRequestAsync)} was invoked.");


            /*
             * Not ideal to use disposable HttpClientHandler and disposable HttpClient but this function is only expected to
             * be called once every 24 hours due to caching. This negates the socket issue that can be encountered by using
             * disposable HttpClients. Can't use DI as need a handler with a potentially new Certificate each request.
             */
            using var handler = new HttpClientHandler();

            var cert = await GetCertificateFromKeyVaultAsync();
            handler.ClientCertificates.Add(cert);
            handler.ServerCertificateCustomValidationCallback = ValidateServerCertificate;
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "*/*");

            logger.LogInformation($"{nameof(DGCGMutualTLSService)}: Calling {$"{settings.BaseUrl}/{endpoint}"}.");
            var response = await retryPolicy.ExecuteAsync(() => client.GetAsync(endpoint));

            if (response.IsSuccessStatusCode)
            {
                var content = (string)await response.Content.ReadAsStringAsync();
                logger.LogInformation($"{nameof(DGCGMutualTLSService)}: successful response.");

                logger.LogTraceAndDebug($"{nameof(DGCGMutualTLSService)}: {nameof(MakeMutuallyAuthenticatedRequestAsync)} has finished.");
                return content;
            }

            logger.LogError($"{nameof(DGCGMutualTLSService)}: Non-success response code {response.StatusCode}: {response}.");
            throw new Exception($"{nameof(DGCGMutualTLSService)}: Non-success response code {response.StatusCode} from {settings.BaseUrl}/{endpoint}.");
        }

        private bool ValidateServerCertificate(
            HttpRequestMessage message,
            X509Certificate2 certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            logger.LogInformation($"{nameof(DGCGMutualTLSService)}: Validating certificate {certificate.Issuer}.");

            if (!string.Equals(certificate.Thumbprint, settings.DGCGTLSCertThumbprint, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError($"{nameof(DGCGMutualTLSService)}: Certificate thumbprint '{certificate.Thumbprint.ToUpper()}' does not match expected thumbprint '{settings.DGCGTLSCertThumbprint.ToUpper()}'.");
                return false;
            }

            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                logger.LogError($"{nameof(DGCGMutualTLSService)}: Certificate error: {sslPolicyErrors}.");
                return false;
            }

            return true;
        }

        private void SetupRetryPolicy()
        {
            List<HttpStatusCode> statusCodes = new List<HttpStatusCode> { HttpStatusCode.InternalServerError, HttpStatusCode.TooManyRequests };
            int retryCount = settings.ApiRetryCount;
            int retrySleepDuration = settings.APIRetrySleepDurationMilliseconds;
            int timeout = settings.APITimeoutMilliseconds;

            retryPolicy = HttpRetryPolicyUtils.CreateRetryPolicyWrapCustomResponseCodes(retryCount, retrySleepDuration, timeout, "retrieving Trust List from DGCG API", logger, statusCodes);
        }

        private async Task<X509Certificate2> GetCertificateFromKeyVaultAsync()
        {
            var kvCertName = settings.ClientCertificateName;
            logger.LogTraceAndDebug($"{nameof(DGCGMutualTLSService)}: Getting Certificate '{kvCertName}' from key vault.");

            var certificate = await certificateCache.GetCertificateByNameAsync(kvCertName, false);
            if (!certificate.HasPrivateKey)
            {
                throw new Exception($"Certificate '{kvCertName}' does not have private key.");
            }

            return certificate;
        }
    }
}
