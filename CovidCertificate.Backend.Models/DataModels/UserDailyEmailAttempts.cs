using CovidCertificate.Backend.Models.Enums;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CovidCertificate.Backend.Models.DataModels
{
    [Collection("UserDailyEmailAttempts")]
	public class UserDailyEmailAttempts : MongoDocument
	{
		[BsonRequired]
		[BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
		public Dictionary<CertificateScenario, DateTime> DatesAttempted;
		[BsonRequired]
		public string UserHash;
		[BsonRequired]
		[BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
		public Dictionary<CertificateScenario, int> Attempts;

		public UserDailyEmailAttempts(Dictionary<CertificateScenario, DateTime> datesAttempted, string userHash, Dictionary<CertificateScenario, int> attempts)
		{
			DatesAttempted = datesAttempted.ToDictionary(d => d.Key, d => d.Value.ToUniversalTime().Date);
			UserHash = userHash;
			Attempts = attempts;
		}
	}
}
