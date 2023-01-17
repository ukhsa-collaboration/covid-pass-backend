using System;

namespace CovidCertificate.Backend.Models.DataModels
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class Collection : Attribute
    {
        public string CollectionName { get; }

        public Collection(string collectionName)
        {
            CollectionName = collectionName;
        }
    }
}
