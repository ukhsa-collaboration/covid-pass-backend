using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IVaccineFilterService
    {
        Task<IEnumerable<Vaccine>> FilterVaccinesByFlagsAsync(List<Vaccine> vaccines, bool shouldFilterFirstAndLast);
    }
}
