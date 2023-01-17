using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IKeyRing
    {
        Task<byte[]> SignDataAsync(string keyId, byte[] data);
        Task<bool> VerifyDataAsync(string keyId, byte[] data, byte[] signature);
        Task<string> GetRandomKeyAsync();
        Task<string> GetKeyByTagAsync(string certificateTag);
    }
}
