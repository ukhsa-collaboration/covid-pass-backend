using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels.OdsModels;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly.Wrap;

namespace CovidCertificate.Backend.Services
{
    public class OdsApiService : IOdsApiService
    {
        private readonly ILogger<OdsApiService> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly OdsApiSettings settings;
        private AsyncPolicyWrap<HttpResponseMessage> retryPolicy;

        public OdsApiService(ILogger<OdsApiService> logger, IHttpClientFactory httpClientFactory, OdsApiSettings settings)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.settings = settings;
            SetupRetryPolicy();
        }

        private void SetupRetryPolicy()
        {
            List<HttpStatusCode> statusCodes = new List<HttpStatusCode> { HttpStatusCode.InternalServerError, HttpStatusCode.TooManyRequests };
            this.retryPolicy = HttpRetryPolicyUtils.CreateRetryPolicyWrapCustomResponseCodes(settings.RetryCount,
                settings.RetrySleepDurationInMilliseconds,
                settings.TimeoutInMilliseconds,
                "retrieving Organisation data from NHS ODS API",
                logger, statusCodes);
        }
        public async Task<OdsApiOrganisationResponse> GetOrganisationFromOdsCodeAsync(string odsCode)
        {
            logger.LogInformation("Trying to get Organisation from ODS Code");

            var endpoint = $"{settings.OdsApiBaseUrl}/{odsCode}";

            var response = await retryPolicy.ExecuteAsync(() => httpClientFactory.CreateClient().GetAsync(endpoint));

            var responseMessage = response.Content != null ? await response.Content.ReadAsStringAsync() : "";

            if(response.IsSuccessStatusCode)
            {
                logger.LogInformation("Success in obtaining Organisation from ODS API.");
                OdsApiOrganisationResponse organisation = JsonConvert.DeserializeObject<OdsApiOrganisationResponse>(responseMessage);
                
                return organisation;
            }

            var message = $"Error during accessing ODS API. ResponseStatusCode: {response.StatusCode}, Error message: '{responseMessage}'.";
            
            throw new APILookupException(message);
        }

        public async Task<OdsApiOrganisationsLastChangeDateResponse> GetOrganisationsUpdatedFromLastChangeDateAsync(string lastChangeDate)
        {
            logger.LogInformation($"Trying to get Organisation changes since {lastChangeDate}.");
            var organisations = new OdsApiOrganisationsLastChangeDateResponse
            {
                Organisations = new List<GPOrganisation>()
            };
            var endpoint = $"{settings.OdsApiBaseUrl}?LastChangeDate={lastChangeDate}&Limit={settings.OdsApiResponseLimit}";
            var httpClient = httpClientFactory.CreateClient();
            while(endpoint != null)
            {
                var response = await retryPolicy.ExecuteAsync(() => httpClient.GetAsync(endpoint));

                var responseMessage = response.Content != null ? await response.Content.ReadAsStringAsync() : "";

                if (!response.IsSuccessStatusCode)
                {
                    var message = $"Error during accessing ODS API. ResponseStatusCode: {response.StatusCode}, Error message: '{responseMessage}'.";

                    throw new APILookupException(message);
                }
                    
                var organisationsFromResponse = JsonConvert.DeserializeObject<OdsApiOrganisationsLastChangeDateResponse>(responseMessage);
                organisations.Organisations.AddRange(organisationsFromResponse.Organisations);
                endpoint = response.Headers.TryGetValues("next-page", out var values) ? values.First() : null;
            }

            logger.LogInformation($"Success in obtaining {organisations.Organisations.Count} Organisations that have changed since {lastChangeDate} from ODS API.");

            return organisations;
        }
    }
}
