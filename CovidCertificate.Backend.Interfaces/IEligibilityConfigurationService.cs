using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IEligibilityConfigurationService
	{
		Task<(string, string)> GetEligibilityConfigurationBlobContainerAndFilenameAsync();
	}
}
