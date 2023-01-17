using CovidCertificate.Backend.Models.DataModels;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace CovidCertificate.Backend.Models.ResponseDtos
{
    [Collection("UserPreference")]
    public class UserPreferenceResponse : MongoDocument
    {
        [BsonRequired]
        public string NHSID { get; private set; }
        public string LanguagePreference { get; set; }
        public DateTime TCAcceptanceDateTime { get; set; }

        public bool AcceptedLatestTC { get; set; }
        public UserPreferenceResponse(string id, string lang)
        {
            NHSID = id;
            LanguagePreference = lang; 
        }
        public UserPreferenceResponse(string id, string lang,DateTime acceptedDateTime)
        {
            NHSID = id;
            LanguagePreference = lang;
            TCAcceptanceDateTime = acceptedDateTime;
        }
        public UserPreferenceResponse(string id, DateTime acceptedDateTime)
        {
            NHSID = id;
            TCAcceptanceDateTime = acceptedDateTime;
            //English default language
            LanguagePreference = "en";
        }

    }
}
