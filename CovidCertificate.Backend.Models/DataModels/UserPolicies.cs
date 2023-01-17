using MongoDB.Bson.Serialization.Attributes;

namespace CovidCertificate.Backend.Models.DataModels
{
    [Collection("User-Policies")]
    public class UserPolicies : MongoDocument
    {
        [BsonRequired]
        public string NhsNumberDobHash { get; private set; }

        public GracePeriod GracePeriod { get; set; }

        public UserPolicies(string nhsNumberDobHash)
        {
            NhsNumberDobHash = nhsNumberDobHash;
        }
    }
}
