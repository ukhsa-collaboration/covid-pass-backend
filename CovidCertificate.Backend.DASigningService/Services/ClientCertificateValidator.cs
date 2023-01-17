using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Models;
using CovidCertificate.Backend.DASigningService.Models.Exceptions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.DASigningService.Services
{
    public class ClientCertificateValidator : IClientCertificateValidator
    {
        private readonly ILogger<ClientCertificateValidator> logger;

        public ClientCertificateValidator(ILogger<ClientCertificateValidator> logger)
        {
            this.logger = logger;
        }

        public void ValidateCertificate(HttpRequest request, ErrorHandler errorHandler, RegionConfig regionConfig)
        {
            var clientThumbprint = request.Headers["X-Client-Certificate-Thumbprint"].ToString().ToUpper();
            if (string.IsNullOrEmpty(clientThumbprint))
            {
                logger.LogError("No client certificate found");
                errorHandler.AddError(ErrorCode.CLIENT_CERTIFICATE_MISSING, "X-Client-Certificate-Thumbprint missing");
                return;
            }

            if (!(regionConfig.AllowedThumbprints?.Contains(clientThumbprint) ?? false))
            {
                errorHandler.AddError(ErrorCode.INVALID_CLIENT_CERTIFICATE, $"Thumbprint ({clientThumbprint}) does not belong to region's ({regionConfig.SubscriptionKeyIdentifier}) allowed thumbprints");
                throw new ThumbprintNotAllowedException($"Thumbprint does not belong to region's ({regionConfig.SubscriptionKeyIdentifier}) allowed thumbprints");
            }
        }
    }
}

