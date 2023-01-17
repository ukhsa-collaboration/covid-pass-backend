using System;
using MongoDB.Bson.Serialization.Attributes;

namespace CovidCertificate.Backend.Models.DataModels
{
    [Collection("UserDailyDomesticPdfAttempt")]
    public class UserDailyDomesticPdfAttempt : MongoDocument
    {
        [BsonRequired] 
        public string UserHash { get; set; }
        [BsonRequired] 
        public DateTime AttemptDateTime { get; set; }
    }
}
