using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Utils.Extensions;

namespace CovidCertificate.Backend.Services
{
    public class EmailLimiter : IEmailLimiter
	{
		private readonly IMongoRepository<UserDailyEmailAttempts> mongoRepository;
		private readonly IConfiguration configuration;

		public EmailLimiter(IMongoRepository<UserDailyEmailAttempts> mongoRepository, IConfiguration configuration)
		{
			this.mongoRepository = mongoRepository;
			this.configuration = configuration;
		}

		public async Task<UserDailyEmailAttempts> GetUserEmailAttempts(CovidPassportUser user)
		{
			var userHash = StringUtils.GetHashValue(user.NhsNumber, user.DateOfBirth);
			var emailAttempts = await mongoRepository.FindOneAsync(x =>  x.UserHash == userHash);
			if (emailAttempts == null)
			{
				var dateDictionary = new Dictionary<CertificateScenario, DateTime>();
				var attemptsDictionary = new Dictionary<CertificateScenario, int>();

				emailAttempts = new UserDailyEmailAttempts(dateDictionary, userHash, attemptsDictionary);
				await mongoRepository.InsertOneAsync(emailAttempts);
			}

			return emailAttempts;
		}

		public bool UserWithinEmailLimit(UserDailyEmailAttempts emailAttempts, CertificateScenario scenario)
		{
			var emailLimit = configuration.GetValue<int?>("EmailLimit") ?? 20;

			var dateAttempted = emailAttempts.DatesAttempted.GetValueOrDefault(scenario);
			var attempts = emailAttempts.Attempts.ContainsKey(scenario) ? emailAttempts.Attempts[scenario] : 0;
			return (dateAttempted != DateTime.UtcNow.Date || attempts < emailLimit);
		}

		public async Task UpdateUserDailyEmailAttempts(UserDailyEmailAttempts attempts, CertificateScenario scenario)
		{
			if (attempts.DatesAttempted.GetValueOrDefault(scenario) != DateTime.UtcNow.Date)
			{
				attempts.DatesAttempted[scenario] = DateTime.UtcNow.Date;
				attempts.Attempts[scenario] = 1;
			}
			else
			{
				attempts.Attempts[scenario] += 1;
			}
			await mongoRepository.ReplaceOneAsync(attempts, doc => doc.UserHash == attempts.UserHash);
		}
	}
}
