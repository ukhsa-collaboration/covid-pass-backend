using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Models;
using Microsoft.Extensions.Logging;
using PeterO.Cbor;

namespace CovidCertificate.Backend.Services.International
{
    public class CBORFlow : ICBORFlow
    {
        private readonly ILogger<CBORFlow> logger;
        private readonly IKeyRing keyRing;
        // Please refer to https://tools.ietf.org/html/rfc8152#section-4.4
        // externalData is added for unprotected field 
        // contextString is for RFC standard, do not change!
        private static string contextString = "Signature1";

        public CBORFlow(ILogger<CBORFlow> logger, IKeyRing keyRing)
        {
            this.logger = logger;
            this.keyRing = keyRing;
        }

        public async Task<byte[]> AddMetaDataToCborAsync(byte[] originalCborBytes, string keyId, string PKICountry)
        {
            (CBORObject cwtObject, byte[]  protectedBytes) = CreateCborObject(originalCborBytes, keyId);

            var signedCborBytes = await SignCborAsync(originalCborBytes, PKICountry, protectedBytes);

            // Add Signature
            cwtObject.Insert(3, signedCborBytes);

            // Tag entire cwtObject with 18, as stated in 4.2 of RFC 8152
            cwtObject = CBORObject.FromObjectAndTag(cwtObject, 18);

            // Convert altered cbor back to bytes
            byte[] alteredCborBytes = cwtObject.EncodeToBytes();

            return alteredCborBytes;
        }

        private static (CBORObject cwtObject, byte[] protectedBytes) CreateCborObject(byte[] originalCborBytes,
            string keyId)
        {
            var cwtObject = CBORObject.NewArray();

            // Add algorithm and KeyId to protected
            CBORObject protectedObject = CBORObject.NewMap();
            // Please refer to https://tools.ietf.org/html/rfc8152
            // Key values such has 1 and 4 in CBOR maps are specifically set in reference to RFC 8152
            // Please do not change key value integers for CBOR maps and arrays

            protectedObject.Add(1, Algorithms.SHA256withECDSA);

            // Convert keyId from human readable string to Base64 string
            // Then convert that base64 string to a base64 byte array
            byte[] keyIdBytes = Convert.FromBase64String(keyId);

            protectedObject.Add(4, keyIdBytes);
            var protectedBytes = protectedObject.EncodeToBytes();
            cwtObject.Insert(0, protectedBytes);

            // Add unprotected header
            CBORObject unprotectedObject = CBORObject.NewMap();
            cwtObject.Insert(1, unprotectedObject);

            // Add Message/Payload
            cwtObject.Insert(2, originalCborBytes);

            return (cwtObject, protectedBytes);
        }

        private async Task<byte[]> SignCborAsync(byte[] originalCborBytes, string PKICountry, byte[] protectedBytes)
        {
            // *** Make seperate object just for creating signature ***
            CBORObject obj = CBORObject.NewArray();
            obj.Add(contextString);
            obj.Add(protectedBytes);
            obj.Add(Array.Empty<byte>());

            obj.Add(originalCborBytes);

            var objBytes = obj.EncodeToBytes();

            // Sign that obj just created to get signedCborBytes

            var key = string.IsNullOrWhiteSpace(PKICountry) ?
                    string.Empty :
                    $"DSC-{PKICountry}";

            // Sign that obj just created to get signedCborBytes
            var signedCborBytes = await keyRing.SignDataAsync(key, objBytes);

            return signedCborBytes;
        }
    }
}
