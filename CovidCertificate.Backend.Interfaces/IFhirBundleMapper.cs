using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IFhirBundleMapper<T> where T:IGenericResult
    {
        Task<IEnumerable<T>> ConvertBundleAsync(Bundle bundle);
    }
}
