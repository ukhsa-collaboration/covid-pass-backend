using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.Interfaces.UserInterfaces;

namespace CovidCertificate.Backend.Interfaces.International
{
    public interface IEncoderService
    {
        Task<string> EncodeFlowAsync(IUserCBORInformation user, long certifiateGenerationTime, IGenericResult result, string uniqueCertificateIdentifier, DateTime? validityEndDate, string PKICountry = null, string cborIssuer = "GB");
    }
}
