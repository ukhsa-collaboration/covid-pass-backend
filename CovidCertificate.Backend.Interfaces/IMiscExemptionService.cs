using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IMiscExemptionService
    {
        Task<bool> IsUserExemptAsync(string nhsNumber, DateTime dateOfBirth);

        Task<IEnumerable<DomesticExemption>> GetExemptionsAsync(string nhsNumber, DateTime dateOfBirth);
    }
}
