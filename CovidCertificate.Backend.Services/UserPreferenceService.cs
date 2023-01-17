using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Utils;

namespace CovidCertificate.Backend.Services
{
    public class UserPreferenceService : IUserPreferenceService
    {
        private readonly IMongoRepository<UserPreferenceResponse> mongoRepository;
        private readonly ILogger<UserPreferenceService> logger;
        private readonly IConfiguration configuration;

        public UserPreferenceService(
	        IMongoRepository<UserPreferenceResponse> mongoRepository, 
	        ILogger<UserPreferenceService> logger, 
	        IConfiguration configuration)
        {
            this.mongoRepository = mongoRepository;
            this.logger = logger;
            this.configuration = configuration;
        }

        public async Task UpdateTermsAndConditionsAsync(string nhsNumberDobHash)
        {
            if (nhsNumberDobHash == null)
            {
                logger.LogError("User does not have an NHS ID");
                throw new ArgumentNullException("User does not have an NHS ID");
            }

            var result = await mongoRepository.FindOneAsync(x => x.NHSID == nhsNumberDobHash);

            if (result is null)
            {
                var document = new UserPreferenceResponse(nhsNumberDobHash, DateTime.UtcNow);
                logger.LogInformation("New preference data was created");
                await mongoRepository.InsertOneAsync(document);
                logger.LogTraceAndDebug($"Preference data{document}");

            }
            else
            {
                result.TCAcceptanceDateTime = DateTime.UtcNow;
                await mongoRepository.ReplaceOneAsync(result, x => x.NHSID == nhsNumberDobHash);
                logger.LogInformation("T&C data updated");
                logger.LogTraceAndDebug($"User with id {nhsNumberDobHash} updated their T&C acceptance at {result.TCAcceptanceDateTime}");
            }
        }

        public async Task UpdateLanguageCodeAsync(string id, string lang)
        {
            if (id == null)
            {
                logger.LogError("User does not have an NHS ID");
                throw new ArgumentNullException("User does not have an NHS ID");
            }
            if (!LanguageUtils.ValidCountryCode(lang))
            {
                logger.LogError($"{lang} is not a valid language code");
                throw new ArgumentException("Not a valid language code");
            }
            var userPreferences = await mongoRepository.FindOneAsync(x => x.NHSID == id);
            if (userPreferences == null)
            {
                var newPreferences = new UserPreferenceResponse(id, lang);
                await mongoRepository.InsertOneAsync(newPreferences);
                logger.LogInformation("New preference data was created");
                logger.LogTraceAndDebug($"Preference data{newPreferences}");
            }
            else
            {
                userPreferences.LanguagePreference = lang;
                await mongoRepository.ReplaceOneAsync(userPreferences, x => x.NHSID == id);
                logger.LogInformation("Language code updated");
                logger.LogTraceAndDebug($"User with id {id} now has language preference {lang}");
            }
        }

        public async Task<UserPreferenceResponse> GetPreferencesAsync(string nhsNumberDobHash)
        {
            if (nhsNumberDobHash == null)
            {
                logger.LogError("User does not have an NHS ID");
                throw new ArgumentNullException("User does not have an NHS ID");
            }

            var userPreferences = await mongoRepository.FindOneAsync(x => x.NHSID == nhsNumberDobHash);
            if (userPreferences == null)
            {
                logger.LogWarning("No preference data found for this user");
                return null;
            }
            var TCDate = configuration.GetValue<DateTime>("TCDate"); //the date the latest T&C have been updated
            logger.LogTraceAndDebug($"T&C last updated {TCDate}");
            if (userPreferences.TCAcceptanceDateTime > TCDate)
            {
                userPreferences.AcceptedLatestTC = true;
            }
            return userPreferences;
        }
    }
}
