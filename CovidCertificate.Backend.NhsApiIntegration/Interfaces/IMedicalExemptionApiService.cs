using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.NhsApiIntegration.Interfaces
{
    public interface IMedicalExemptionApiService
    {
        Task<IEnumerable<MedicalExemption>> GetMedicalExemptionDataAttendedAsync(string identityToken);

        Task<IEnumerable<MedicalExemption>> GetMedicalExemptionDataUnattendedAsync(string nhsNumber);
    }
}
