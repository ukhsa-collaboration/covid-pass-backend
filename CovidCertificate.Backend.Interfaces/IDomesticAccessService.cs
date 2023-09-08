using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IDomesticAccessService
    {
        Task<DomesticAccessLevel> GetDomesticAccessLevelAsync(DateTime dateOfBirth);
    }
}
