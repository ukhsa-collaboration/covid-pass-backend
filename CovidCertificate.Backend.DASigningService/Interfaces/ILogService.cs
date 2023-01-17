using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.Models;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.DASigningService.Interfaces
{
    public interface ILogService
    {
        Task LogResultAsync(ILogger logger, string uvci, string apiName, HttpStatusCode httpCode, RegionConfig regionConfig);
    }
}
