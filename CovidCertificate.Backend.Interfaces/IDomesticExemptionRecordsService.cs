using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IDomesticExemptionRecordsService
    {
        Task<IEnumerable<DomesticExemptionRecord>> GetDomesticExemptionsAsync(string nhsNumber, DateTime dateOfBirth);

        Task<bool> SaveDomesticExemptionAsync(DomesticExemptionRecord exemption, bool isMedicalExemption);

        Task RemoveDomesticExemptionsForUserAsync(string nhsNumberDobHash, bool isMedicalExemption);
    }
}
