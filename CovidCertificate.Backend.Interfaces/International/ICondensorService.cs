using System;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.Interfaces.UserInterfaces;
using PeterO.Cbor;

namespace CovidCertificate.Backend.Interfaces.International
{
    public interface ICondensorService
    {
        CBORObject CondenseCBOR(IUserCBORInformation user, long certifiateGenerationTime, IGenericResult result, string uniqueCertificateIdentifier, DateTime? validityEndDate, string barcodeIssuerCountry = null);
    }
}
