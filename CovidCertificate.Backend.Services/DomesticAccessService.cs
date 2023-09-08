using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace CovidCertificate.Backend.Services
{
    public class DomesticAccessService : IDomesticAccessService
    {
        private readonly IConfiguration configuration;
        private readonly IFeatureManager featureManager;

        public DomesticAccessService(IConfiguration configuration,
            IFeatureManager featureManager)
        {
            this.configuration = configuration;
            this.featureManager = featureManager;
        }

        public async Task<DomesticAccessLevel> GetDomesticAccessLevelAsync(DateTime dateOfBirth)
        {
            var u12TravelPass = await featureManager.IsEnabledAsync(FeatureFlags.U12TravelPass);

            if (u12TravelPass && U12TravelPassAge(dateOfBirth))
            {
                return DomesticAccessLevel.U12;
            }

            var ageBasedDomesticAccessIsEnabled = await featureManager.IsEnabledAsync(FeatureFlags.DomesticPassAgeLimit);

            if (ageBasedDomesticAccessIsEnabled && UnderDomesticPassAccessAge(dateOfBirth))
            {
                return DomesticAccessLevel.NoAccess;
            }

            return DomesticAccessLevel.Access;
        }

        private bool U12TravelPassAge(DateTime dateOfBirth)
        {
            var U12AccessAge = Int32.Parse(configuration["U12AccessAge"]);

            return DateUtils.AgeIsBelowLimit(dateOfBirth, U12AccessAge);
        }

        private bool UnderDomesticPassAccessAge(DateTime dateOfBirth)
        {
            var minDomesticAccessAge = Int32.Parse(configuration["MinimumDomesticAccessAge"]);

            return DateUtils.AgeIsBelowLimit(dateOfBirth, minDomesticAccessAge);

        }
    }
}
