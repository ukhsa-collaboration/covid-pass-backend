using System.Collections.Generic;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Models.ResponseDtos
{
    public class QRcodeResponse
    {
        public string ValidityEndDate { get; set; }
        public string EligibilityEndDate { get; set; }
        public string UniqueCertificateIdentifier { get; set; }
        public IEnumerable<IGenericResult> ResultData { get; private set; }
        public QRResponseType QRType { get; set; }

        public QRcodeResponse(string validityEndDate, IEnumerable<IGenericResult> resultData, string uniqueCertificateIdentifier, QRResponseType qrType, string eligibilityEndDate = "") 
        {
            ValidityEndDate = validityEndDate;
            ResultData = resultData;
            UniqueCertificateIdentifier = uniqueCertificateIdentifier;
            QRType = qrType;
            EligibilityEndDate = eligibilityEndDate;
        }
    }
}
