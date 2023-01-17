using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CovidCertificate.Backend.Services
{
    public class UpdateOrganisationsService : IUpdateOrganisationsService
    {
        private const string LastChangeDateAppParamKey = "ODSLastChangeDate";

        private readonly ILogger logger;
        private readonly IOdsApiService odsApiService;
        private readonly IMongoRepository<OdsCodeCountryModel> odsCodeCountryRepository;
        private readonly IMongoRepository<ApplicationParametersModel> applicationParametersRepository;

        public UpdateOrganisationsService(
            IOdsApiService odsApiService, 
            IMongoRepository<OdsCodeCountryModel> odsCodeCountryRepository, 
            IMongoRepository<ApplicationParametersModel> applicationParametersRepository, 
            ILogger<UpdateOrganisationsService> logger)
        {
            this.odsApiService = odsApiService;
            this.odsCodeCountryRepository = odsCodeCountryRepository;
            this.applicationParametersRepository = applicationParametersRepository;
            this.logger = logger;
        }

        public async Task UpdateOrganisationsFromOdsAsync()
        {
            logger.LogInformation($"Getting appParam by {LastChangeDateAppParamKey} key.");

            var appParam = await applicationParametersRepository.FindOneAsync(x => x.Key == LastChangeDateAppParamKey);

            if (string.IsNullOrEmpty(appParam?.Value))
            {
                logger.LogInformation("Value of 'LastChangeDate' in Db was null or empty. Creating new object.");

                appParam = new ApplicationParametersModel(LastChangeDateAppParamKey,
                    DateTime.UtcNow.AddDays(-184).ToString(DateUtils.LastChangeDateFormat)); // 185 days is max time period to ask API about
            }

            var odsOrganisationApiResponse = await odsApiService.GetOrganisationsUpdatedFromLastChangeDateAsync(appParam?.Value);
            logger.LogInformation($"Updating {odsOrganisationApiResponse?.Organisations?.Count} organisations from ODS API");

            var odsCodes = odsOrganisationApiResponse.Organisations.Select(x => x.OrgId).ToList();

            await UpdateAllOdsCodesInDatabaseAsync(odsCodes);

            await UpdateLastChangedDateAsync(appParam);
            logger.LogInformation("Update of organisations was successful.");
        }

        private async Task UpdateAllOdsCodesInDatabaseAsync(List<string> odsCodes)
        {
            var throttler = new SemaphoreSlim(initialCount: 8, maxCount: 8);

            var tasks = odsCodes.Select(async odsCode =>
            {
                await throttler.WaitAsync();

                try
                {
                    await UpdateOdsCodeAsync(odsCode);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task UpdateOdsCodeAsync(string odsCode)
        {
            var res = await odsApiService.GetOrganisationFromOdsCodeAsync(odsCode);

            if (string.IsNullOrEmpty(res?.Organisation?.GeoLoc?.Location?.Country))
            {
                logger.LogError($"Cannot update country because Organisation with name: {res?.Organisation?.Name} is empty or null.");

                return;
            }

            var country = res.Organisation.GeoLoc.Location.Country;
            var odsUpdate = Builders<OdsCodeCountryModel>.Update
                .Set(x => x.OdsCode, odsCode)
                .Set(x => x.Country, country)
                .Set(x => x.LastUpdated, DateTime.UtcNow.ToString(DateUtils.LastChangeDateFormat));
            
            logger.LogInformation($"Updating country of organization {res?.Organisation?.Name} to {country}.");

            await odsCodeCountryRepository.UpdateOneAsync(odsUpdate, x => x.OdsCode == odsCode, true);
        }

        private async Task UpdateLastChangedDateAsync(ApplicationParametersModel appParam)
        {
            var utcNow = DateTime.UtcNow;
            var nowString = utcNow.ToString(DateUtils.LastChangeDateFormat);

            appParam.Value = nowString;
            appParam.LastUpdatedUtc = utcNow;

            logger.LogInformation($"{nameof(UpdateLastChangedDateAsync)}: appParam.Value is {appParam.Value}, appParam.LastUpdatedUtc is {appParam.LastUpdatedUtc}.");

            await applicationParametersRepository.ReplaceOneAsync(appParam, isUpsert: true);
        }
    }
}
