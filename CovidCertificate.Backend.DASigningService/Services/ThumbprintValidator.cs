using System;
using System.Linq;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Models.Exceptions;
using CovidCertificate.Backend.Utils.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.DASigningService.Services
{
    public class ThumbprintValidator : IThumbprintValidator
    {
        private readonly ILogger<ThumbprintValidator> logger;
        private readonly IRegionConfigService regionConfigService;

        public ThumbprintValidator(
            ILogger<ThumbprintValidator> logger,
            IRegionConfigService regionConfigService)
        {
            this.logger = logger;
            this.regionConfigService = regionConfigService;
        }

        public void ValidateThumbprint(HttpRequest request, ErrorHandler errorHandler)
        {
            var regionConfig =
                regionConfigService.GetRegionConfig(
                    request.Headers[HeaderConsts.RegionSubscriptionNameHeader],
                    errorHandler);

            var clientThumbprint = request.Headers["X-Client-Certificate-Thumbprint"].ToString();
            if (string.IsNullOrEmpty(clientThumbprint))
            {
                logger.LogError("No client certificate found");
                errorHandler.AddError(ErrorCode.CLIENT_CERTIFICATE_MISSING, "X-Client-Certificate-Thumbprint missing");

                return;
            }

            if (!(regionConfig.AllowedThumbprints?.Contains(clientThumbprint, StringComparer.OrdinalIgnoreCase) ?? false))
            {
                errorHandler.AddError(ErrorCode.INVALID_CLIENT_CERTIFICATE, $"Thumbprint ({clientThumbprint}) does not belong to region's ({regionConfig.SubscriptionKeyIdentifier}) allowed thumbprints - {string.Join(',', regionConfig.AllowedThumbprints)}");
                throw new ThumbprintNotAllowedException($"Thumbprint ({clientThumbprint}) does not belong to region's ({regionConfig.SubscriptionKeyIdentifier}) allowed thumbprints - {string.Join(',', regionConfig.AllowedThumbprints)}");
            }

            logger.LogDebug("Thumbprint Validated.");
        }
    }
}

