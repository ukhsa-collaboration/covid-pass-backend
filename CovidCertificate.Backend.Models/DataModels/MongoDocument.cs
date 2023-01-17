using CovidCertificate.Backend.Models.Interfaces;
using MongoDB.Bson;
using System;

namespace CovidCertificate.Backend.Models.DataModels
{
    public abstract class MongoDocument : IMongoDocument
    {
        protected MongoDocument() => Id = ObjectId.GenerateNewId();

        protected MongoDocument(string id)
        {
            if (!ObjectId.TryParse(id, out var documentId))
                throw new ArgumentException($"Could not create a MongoDocument id from the specified id string: {id}");

            Id = documentId;
        }

        public ObjectId Id { get; set; }
    }
}
