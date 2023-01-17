using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IIsolationExemptionStatusService
    {
        Task<IsolationExemptionStatus> GetIsolationExemptionStatusAsync(CovidPassportUser patient, DateTime effectiveDateTime, string apiKey);
    }
}
