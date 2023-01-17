using System;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.PdfLimiters;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.Extensions.Configuration;

namespace CovidCertificate.Backend.Services.PdfLimiters
{
    public class InternationalPdfLimiter : IInternationalPdfLimiter
    {
        private const string MaxUserDailyInternationalPdfAttemptsSettingsKey = "MaxUserDailyInternationalPdfAttempts";

        private readonly IMongoRepository<UserDailyInternationalPdfAttempt> userDailyAttemptsMongoRepository;
        private readonly IConfiguration configuration;

        public InternationalPdfLimiter(IMongoRepository<UserDailyInternationalPdfAttempt> userDailyAttemptsMongoRepository, IConfiguration configuration)
        {
            this.userDailyAttemptsMongoRepository = userDailyAttemptsMongoRepository;
            this.configuration = configuration;
        }

        public async Task<(bool isUserAllowed, int retryAfterSeconds)> GetUserAllowanceAndRetryTimeForInternationalPdfAsync(CovidPassportUser user)
        {
            var attempts = (await userDailyAttemptsMongoRepository.FindAllAsync(
                    x => x.UserHash == user.ToNhsNumberAndDobHashKey()))
                .OrderBy(x => x.AttemptDateTime).ToList();

            var internationalPdfsLimit = configuration.GetValue<int?>(MaxUserDailyInternationalPdfAttemptsSettingsKey) ?? 10;

            if (attempts.FirstOrDefault() is null || attempts.Count < internationalPdfsLimit)
            {
                return (true, 0);
            }

            var retryAfterSeconds = (int)(attempts.First().AttemptDateTime.AddHours(24) - DateTime.UtcNow).TotalSeconds;

            return (false, retryAfterSeconds);
        }

        public async Task AddUserDailyInternationalPdfAttemptAsync(CovidPassportUser user)
        {
            var userDailyAttempt = new UserDailyInternationalPdfAttempt
            {
                UserHash = user.ToNhsNumberAndDobHashKey(),
                AttemptDateTime = DateTime.UtcNow
            };

            await userDailyAttemptsMongoRepository.InsertOneAsync(userDailyAttempt);
        }
    }
}
