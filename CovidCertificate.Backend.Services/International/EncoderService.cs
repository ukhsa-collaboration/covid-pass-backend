using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.Interfaces.UserInterfaces;
using PeterO.Cbor;

namespace CovidCertificate.Backend.Services.International
{
    public class EncoderService : IEncoderService
    {
        private readonly ICondensorService condensor;
        private readonly ICBORFlow cborFlow;
        private readonly IKeyRing keyRing;

        public EncoderService(ICondensorService condensor, ICBORFlow cborFlow, IKeyRing keyRing)
        {
            this.condensor = condensor;
            this.cborFlow = cborFlow;
            this.keyRing = keyRing;
        }

        public async Task<string> EncodeFlowAsync(IUserCBORInformation user, long certifiateGenerationTime, IGenericResult result, string uniqueCertificateIdentifier, DateTime? validityEndDate, string PKICountry = "", string cborIssuer = "GB")
        {
            var certificateTag = PKICountry == "GB" ? "DSC-ENG-WAL" : $"DSC-{PKICountry}";
            CBORObject condensedCbor = condensor.CondenseCBOR(user, certifiateGenerationTime, result, uniqueCertificateIdentifier, validityEndDate, cborIssuer);
            var keyId = string.IsNullOrWhiteSpace(PKICountry) ? await keyRing.GetRandomKeyAsync() : await keyRing.GetKeyByTagAsync(certificateTag);
            byte[] originalCborBytes = condensedCbor.EncodeToBytes();
            byte[] alteredCborBytes = await cborFlow.AddMetaDataToCborAsync(originalCborBytes, keyId, PKICountry);
            //ZLib Compression
            byte[] compressedSignedCborBytes = await ZlibCompression.CompressData(alteredCborBytes);
            //Base45 Encode
            string encodedString = Base45Encoding.Encode(compressedSignedCborBytes);
            // Add HC1 to start of string
            encodedString = $"HC1:{encodedString}";

            return encodedString;
        }
    }
}
