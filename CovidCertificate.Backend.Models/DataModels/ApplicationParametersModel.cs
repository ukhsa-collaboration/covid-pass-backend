using System;
using MongoDB.Bson.Serialization.Attributes;

namespace CovidCertificate.Backend.Models.DataModels
{
    [Collection("ApplicationParameters")]
    public class ApplicationParametersModel : MongoDocument
    {
        [BsonRequired]
        public string Key { get; set; }

        [BsonRequired]
        public string Value { get; set; }

        [BsonRequired]
        public DateTime LastUpdatedUtc { get; set; }

        public ApplicationParametersModel(string key, string value)
        {
            this.Key = key;
            this.Value = value;
            this.LastUpdatedUtc = DateTime.UtcNow;
        }
    }
}
