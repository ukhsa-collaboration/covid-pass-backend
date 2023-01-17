using System;
using MongoDB.Bson.Serialization.Attributes;

namespace CovidCertificate.Backend.Models.DataModels
{
    [Collection("UserDailyOtpAttempt")]
    public class UserDailyOtpAttempt : MongoDocument
    {
        [BsonRequired] 
        public string PhoneNumberHash { get; set; }
        [BsonRequired]
        public int NumberOfGeneratedOtps { get; set; }
        [BsonRequired] 
        public DateTime FirstAttemptDate { get; set; }
    }
}
