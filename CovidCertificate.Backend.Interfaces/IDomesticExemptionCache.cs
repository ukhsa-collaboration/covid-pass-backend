using CovidCertificate.Backend.Models.DataModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IDomesticExemptionCache
    {
        public Task<IDictionary<string, IEnumerable<DomesticExemptionRecord>>> GetDomesticExemptionsAsync();
    }
}
