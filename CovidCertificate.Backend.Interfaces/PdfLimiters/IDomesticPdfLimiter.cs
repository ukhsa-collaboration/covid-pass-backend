using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Interfaces.PdfLimiters
{
    public interface IDomesticPdfLimiter
    {
        Task<(bool isUserAllowed, int retryAfterSeconds)> GetUserAllowanceAndRetryTimeForDomesticPdfAsync(CovidPassportUser user);

        Task AddUserDailyDomesticPdfAttemptAsync(CovidPassportUser user);
    }
}
