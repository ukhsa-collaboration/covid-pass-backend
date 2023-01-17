using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IUpdateOrganisationsService
    {
        Task UpdateOrganisationsFromOdsAsync();
    }
}
