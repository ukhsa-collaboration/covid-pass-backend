using System;
using CovidCertificate.Backend.Utils;
using MongoDB.Bson.Serialization.Attributes;

namespace CovidCertificate.Backend.Models.DataModels
{
    [Collection("ODSCodeCountry")]
    public class OdsCodeCountryModel : MongoDocument
    {
        [BsonRequired]
        public string OdsCode { get; set; }
        
        [BsonRequired]
        public string Country { get; set; }

        [BsonRequired]
        public string LastUpdated { get; set; }

        public OdsCodeCountryModel(string odsCode, string country)
        {
            this.OdsCode = odsCode;
            this.Country = country;
            this.LastUpdated = DateTime.UtcNow.ToString(DateUtils.LastChangeDateFormat);
        }
    }
}
