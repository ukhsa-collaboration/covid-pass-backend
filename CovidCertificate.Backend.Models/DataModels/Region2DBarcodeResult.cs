using System;
using MongoDB.Bson.Serialization.Attributes;

namespace CovidCertificate.Backend.Models.DataModels
{
    [Collection("RegionBarcodeResult")]
    public class Region2DBarcodeResult : MongoDocument
    {        
        public string UniqueCertificateId { get; set; }

        [BsonRequired]
        public int HttpStatusCode { get; set; }

        [BsonRequired]
        public DateTime Timestamp { get; set; }

        [BsonRequired]
        public string RegionCode { get; set; }

        public Region2DBarcodeResult(string uvci, int httpStatusCode, DateTime timestamp, string regionCode)
        {
            UniqueCertificateId = uvci;
            HttpStatusCode = httpStatusCode;
            Timestamp = timestamp;
            RegionCode = regionCode;
        }
    }
}
