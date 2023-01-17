using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CovidCertificate.Backend.Models.Interfaces
{
    public interface IMongoDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ObjectId Id { get; set; }
    }
}
