using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
	public interface IEmailLimiter
	{
		Task<UserDailyEmailAttempts> GetUserEmailAttempts(CovidPassportUser user);

		bool UserWithinEmailLimit(UserDailyEmailAttempts emailAttempts, CertificateScenario scenario);

		Task UpdateUserDailyEmailAttempts(UserDailyEmailAttempts emailAttempts, CertificateScenario scenario);
	}
}
