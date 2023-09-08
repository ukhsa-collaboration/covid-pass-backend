using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IOdsCodeService
    {
        Task<string> GetCountryFromOdsCodeAsync(string odsCode);
    }
}
