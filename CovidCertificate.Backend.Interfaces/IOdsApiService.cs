using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels.OdsModels;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IOdsApiService
    {
        Task<OdsApiOrganisationsLastChangeDateResponse> GetOrganisationsUpdatedFromLastChangeDateAsync(String lastChangeDate);

        Task<OdsApiOrganisationResponse> GetOrganisationFromOdsCodeAsync(string odsCode);
    }
}
