using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.ResponseDtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Interfaces.Certificates
{
    public interface ICovidCertificateBuilder
    {
        /// <summary>
        /// Creates a certificate from a set of results associated with a user
        /// </summary>
        /// <param name="allTestResults"></param>
        /// <param name="user"></param>
        /// <param name="scenario"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Task<CertificatesContainer> BuildCertificatesFromResultsAsync(List<IGenericResult> allTestResults, CovidPassportUser user, CertificateScenario scenario, CertificateType? type = null);
    }
}
