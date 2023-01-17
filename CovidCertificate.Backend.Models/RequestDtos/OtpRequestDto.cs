
using System;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.Attributes;

namespace CovidCertificate.Backend.Models.ResponseDtos
{
    [Collection("OtpCodesForLetterService")]
    public class OtpRequestDto : MongoDocument
    { 
        public OtpRequestDto(string phoneNumberHash, string otpCode, int attemptsLeft, bool isFinalGenerated, bool isStillValid, DateTime createdAt)
        {
            PhoneNumberHash = phoneNumberHash;
            OtpCode = otpCode;
            AttemptsLeft = attemptsLeft;
            IsFinalGenerated = isFinalGenerated;
            IsStillValid = isStillValid;
            CreatedAt = createdAt;
        }
        [BsonRequired]
        public string PhoneNumberHash;

        [BsonRequired]
        public string OtpCode;

        [BsonRequired]
        public int AttemptsLeft;

        public bool? IsFinalGenerated;

        public bool? IsStillValid;

        public DateTime? CreatedAt;
    }

}
