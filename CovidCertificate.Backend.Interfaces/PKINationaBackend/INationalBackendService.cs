using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.PKINationalBackend;
using CovidCertificate.Backend.Models.PKINationalBackend.DomesticPolicy;

namespace CovidCertificate.Backend.Interfaces.PKINationaBackend
{
    public interface INationalBackendService
    {
        Task<DGCGTrustList> GetTrustListCertificatesAsync(string kid = null, string country = null);

        Task<IEnumerable<TrustListSubjectPublicKeyInfoDto>> GetSubjectPublicKeyInfoDtosAsync();

        /// <summary>
        /// Returns the current EU Value Set.
        /// </summary>
        /// <param name="lastTimeChecked">
        /// The last time the caller called this function. Null is returned
        /// if the caller has called this function after the last call
        /// to the EUDGCG.
        /// </param>
        /// <param name="includeNonEUValues">
        /// Include extra values used by the NHS Covid Verifier that are
        /// not contained within the EUDGCG.
        /// </param>
        /// <returns>The EU Value set.</returns>
        Task<EUValueSetResponse> GetEUValueSetResponseAsync(bool includeNonEUValues);

        /// <summary>
        /// Returns the current domestic policy.
        /// </summary>
        /// <param name="lastUpdated">
        /// The last time the caller called this function. Null is returned
        /// if the caller has called this function after the last updated to the
        /// policy.
        /// </param>
        /// <returns>The current domestic policy.</returns>
        Task<DomesticPolicyInformation> GetDomesticPolicyInformationAsync(DateTime lastTimeChecked);
    }
}
