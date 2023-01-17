using CovidCertificate.Backend.Models.DataModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IVaccineService
    {
        Task<List<Vaccine>> GetAttendedVaccinesAsync(string idToken, CovidPassportUser covidUser, string apiKey, bool shouldFilterFirstAndLast = false);
        Task<List<Vaccine>> GetUnattendedVaccinesAsync(CovidPassportUser covidUser, string apiKey, bool shouldFilterFirstAndLast = false, bool checkBundleBirthdate = false);
    }
}
