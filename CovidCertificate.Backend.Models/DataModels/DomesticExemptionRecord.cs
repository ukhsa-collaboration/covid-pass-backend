using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Utils;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels
{
    [BsonIgnoreExtraElements]
    [Collection("DomesticExemptions")]
    public class DomesticExemptionRecord : MongoDocument
    {
        public string NhsDobHash { get; private set; }
        public string Reason { get; private set; }
        public bool IsMedicalExemption { get; set; }

        [JsonConstructor]
        public DomesticExemptionRecord(string id, [JsonProperty("nhsDobHash")] string nhsDobHash, [JsonProperty("reason")] string reason) : base(id)
        {
            NhsDobHash = nhsDobHash;
            Reason = reason;
        }

        public DomesticExemptionRecord(DomesticExemptionDto DomesticExemptionDto)
        {
            var hash = HashUtils.GenerateHash(DomesticExemptionDto.NhsNumber, DomesticExemptionDto.DateOfBirth);
            Reason = DomesticExemptionDto.Reason;
            NhsDobHash = hash;
        }

        public DomesticExemptionRecord(string nhsDobHash, string reason)
        {
            NhsDobHash = nhsDobHash;
            Reason = reason;
        }
    }
}
