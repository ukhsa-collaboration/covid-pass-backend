using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces.International
{
    public interface ICBORFlow
    {
        Task<byte[]> AddMetaDataToCborAsync(byte[] originalCborBytes, string keyId, string barcodeIssuerCountry);
    }
}
