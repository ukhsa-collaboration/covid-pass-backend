using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Models.PKINationalBackend;
using CovidCertificate.Backend.PKINationalBackend.Models;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Services.PKINationaBackend
{
    public class TrustListService : ITrustListService
    {
        private readonly DGCGSettings settings;
        private readonly IDGCGMutualTLSService mutualTLSService;
        private readonly ILogger<TrustListService> logger;

        public TrustListService(DGCGSettings dGCGSettings,
                                 IDGCGMutualTLSService mutualTLSService,
                                 ILogger<TrustListService> logger)
        {
            this.settings = dGCGSettings;            
            this.mutualTLSService = mutualTLSService;
            this.logger = logger;
        }

        public async Task<DGCGTrustList> GetDGCGTrustListAsync()
        {
            logger.LogTraceAndDebug($"{nameof(TrustListService)}: {nameof(GetDGCGTrustListAsync)} was invoked.");

            var endpoint = settings.TrustListEndpoint;
            var response = await mutualTLSService.MakeMutuallyAuthenticatedRequestAsync(endpoint);
            var certificates = JsonConvert.DeserializeObject<IEnumerable<DocumentSignerCertificate>>(response)
                                          .Where(x => String.Equals(x.CertificateType, "DSC", StringComparison.OrdinalIgnoreCase));
            var trustList = new DGCGTrustList(certificates);

            logger.LogTraceAndDebug($"{nameof(TrustListService)}: {nameof(GetDGCGTrustListAsync)} has finished.");
            return trustList;
        }        
    }
}
