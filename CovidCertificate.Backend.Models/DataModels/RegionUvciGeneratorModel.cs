using CovidCertificate.Backend.Models.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Models.DataModels
{
    [Collection("RegionUvci")]
    public class RegionUVCIGeneratorModel : MongoDocument, IUVCIGeneratorModel
    {
        [BsonRequired]
        public string UniqueCertificateId { get; set; }
        public CertificateType CertificateType { get; set; }
        public CertificateScenario CertificateScenario { get; set; }
        public string CertificateIssuer { get; set; }
        public string UserHash { get; set; }
        public DateTime DateOfCertificateCreation { get; set; }
        public DateTime DateOfCertificateExpiration { get; set; }
    }
}
