using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IClinicalTrialExemptionService
    {
        Task<bool> IsUserClinicalTrialExemptAsync(string nhsNumber, DateTime dateOfBirth);

        Task<IEnumerable<DomesticExemption>> GetClinicalTrialExemptionsAsync(string nhsNumber, DateTime dateOfBirth);
    }
}
