using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CovidCertificate.Backend.Services.GracePeriodServices
{
    public class GracePeriodService : IGracePeriodService
    {
        private readonly ILogger<GracePeriodService> logger;
        private readonly IGracePeriodCache gracePeriodCache;
        private readonly GracePeriodSettings settings;
        private readonly IMongoRepository<UserPolicies> mongoRepository;
        private readonly IFeatureManager featureManager;

        public GracePeriodService(ILogger<GracePeriodService> logger, IGracePeriodCache gracePeriodCache, GracePeriodSettings settings, IMongoRepository<UserPolicies> mongoRepository, IFeatureManager featureManager)
        {
            this.logger = logger;
            this.gracePeriodCache = gracePeriodCache;
            this.settings = settings;
            this.mongoRepository = mongoRepository;
            this.featureManager = featureManager;
        }

        public async Task<GracePeriodResponse> GetGracePeriodAsync(string nhsNumberDobHash)
        {
            logger.LogInformation($"{nameof(GetGracePeriodAsync)} was invoked");

            var currentGracePeriod = await gracePeriodCache.GetGracePeriodAsync(nhsNumberDobHash);
            var userHasExistingGracePeriod = currentGracePeriod != default;

            logger.LogInformation($"{nameof(GetGracePeriodAsync)} has finished");

            var isDomesticEnabled = await featureManager.IsEnabledAsync(FeatureFlags.EnableDomestic);

            if (!isDomesticEnabled && !userHasExistingGracePeriod)
            {
                return new GracePeriodResponse(false, false, DateTime.UtcNow.AddHours(-96), 72);
            }

            return userHasExistingGracePeriod ?
                GetRemainingGracePeriod(currentGracePeriod) :
                await StartNewGracePeriodAsync(nhsNumberDobHash);
        }

        public async Task<GracePeriodResponse> ResetGracePeriodAsync(string nhsNumberDobHash)
        {
            logger.LogInformation($"{nameof(ResetGracePeriodAsync)} was invoked");

            var userPolicies = await mongoRepository.FindOneAsync(x => x.NhsNumberDobHash == nhsNumberDobHash);
            var currentGracePeriod = userPolicies?.GracePeriod;

            if (currentGracePeriod == default)
            {
                throw new BadRequestException("User needs to have started a grace period to be able to reset it.");
            }

            var newGracePeriod = await StartNewGracePeriodAsync(nhsNumberDobHash, userPolicies);

            logger.LogInformation($"{nameof(ResetGracePeriodAsync)} has finished");

            return newGracePeriod;
        }

        private GracePeriodResponse GetRemainingGracePeriod(GracePeriod gracePeriod)
        {
            return new GracePeriodResponse(true, false, gracePeriod.StartedOn, settings.CountdownTimeInHours);
        }

        private async Task<GracePeriodResponse> StartNewGracePeriodAsync(string nhsNumberDobHash, UserPolicies userPolicies = null)
        {
            logger.LogInformation($"{nameof(StartNewGracePeriodAsync)} was invoked");

            if (userPolicies == default)
            {
                userPolicies = await mongoRepository.FindOneAsync(x => x.NhsNumberDobHash == nhsNumberDobHash);
            }

            var newGracePeriod = new GracePeriod()
            {
                StartedOn = DateTime.UtcNow
            };

            if (userPolicies == default)
            {
                userPolicies = new UserPolicies(nhsNumberDobHash);
                userPolicies.GracePeriod = newGracePeriod;
                await mongoRepository.InsertOneAsync(userPolicies);
            }
            else
            {
                userPolicies.GracePeriod = newGracePeriod;

                await mongoRepository.ReplaceOneAsync(userPolicies, policy => policy.NhsNumberDobHash == nhsNumberDobHash);
            }

            logger.LogInformation($"{nameof(StartNewGracePeriodAsync)} has finished");

            await gracePeriodCache.AddToCacheAsync(newGracePeriod, nhsNumberDobHash);

            return new GracePeriodResponse(true, true, newGracePeriod.StartedOn, settings.CountdownTimeInHours);
        }

    }
}
