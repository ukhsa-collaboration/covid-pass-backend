using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface ICertificateCache
    {
        Task<X509Certificate2> GetCertificateByNameAsync(string certificateName, bool importPrivateKey = true);
        Task<X509Certificate2> GetCertificateByTagAsync(string certificateTag, bool importPrivateKey = true);
    }
}
