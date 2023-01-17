using System;
using CovidCertificate.Backend.Interfaces.ModelVersioning;

namespace CovidCertificate.Backend.Services.ModelVersioning
{
    public class ModelVersionService : IModelVersionService
    {
        public string ModelVersion { get; }

        public ModelVersionService()
        {
            var commitId = Environment.GetEnvironmentVariable("CommitId");

            if (string.IsNullOrEmpty(commitId))
            {
                throw new Exception("CommitId is null or empty.");
            }

            ModelVersion = commitId;
        }
    }
}
