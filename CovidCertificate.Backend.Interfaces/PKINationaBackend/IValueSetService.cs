using System.Threading.Tasks;
using CovidCertificate.Backend.Models.PKINationalBackend;

namespace CovidCertificate.Backend.Interfaces.PKINationaBackend
{
    public interface IValueSetService
    {
        /// <summary>
        /// Returns the latest EU Value Set from the EU Digital Green Card Gateway along with the
        /// extra values used by the NHS Covid Verifier.
        /// </summary>
        /// <returns>Returns 2 EUValueSets. The first is the EU Value Set from the EUDGCG. The 
        /// second is the extra value and is received from blob storage.
        /// </returns>
        Task<(EUValueSet, EUValueSet)> GetEUValueSetAsync();
    }
}
