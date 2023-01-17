using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Interfaces.PdfLimiters
{
    public interface IInternationalPdfLimiter
    {
        Task<(bool isUserAllowed, int retryAfterSeconds)> GetUserAllowanceAndRetryTimeForInternationalPdfAsync(CovidPassportUser user);

        Task AddUserDailyInternationalPdfAttemptAsync(CovidPassportUser user);
    }
}
