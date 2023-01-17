using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.Certificates.UVCI
{
    public class UVCIRepository<T> : IUVCIRepository<T> where T: IMongoDocument, IUVCIGeneratorModel, new()
    {
        private readonly IMongoRepository<T> ucviMongoRepository;
        private readonly ILogger<UVCIRepository<T>> logger;

        public UVCIRepository(ILogger<UVCIRepository<T>> logger,
            IMongoRepository<T> ucviMongoRepository
        )
        {
            this.logger = logger;
            this.ucviMongoRepository = ucviMongoRepository;
        }

        public async Task<bool> UvciExistsAsync(CovidPassportUser user, CertificateScenario scenario)
        {
            logger.LogTraceAndDebug($"UvciExistsAsync was invoked");

            var userHash = StringUtils.GetHashValue(user.Name, user.DateOfBirth);
            var result = await ucviMongoRepository.FindOneAsync(x => x.UserHash == userHash && x.CertificateScenario == scenario);

            logger.LogTraceAndDebug($"UvciExistsAsync finished");
            return result != null;
        }

        public async Task<bool> InsertUvciAsync(string uvci, DateTime certificateGenerationDateTime, GenerateAndInsertUvciCommand command)
        {
            logger.LogTraceAndDebug($"InsertUvciAsync was invoked");

            var result = await ucviMongoRepository.FindOneAsync(x => x.UniqueCertificateId == uvci);

            if (result is null)
            {
                T ucviModel = new T()
                {
                    UniqueCertificateId = uvci,
                    CertificateType = command.CertificateType,
                    CertificateScenario = command.CertificateScenario,
                    CertificateIssuer = command.IssuingInstituion,
                    UserHash = command.UserHash,
                    DateOfCertificateCreation = certificateGenerationDateTime,
                    DateOfCertificateExpiration = command.DateOfCertificateExpiration
                };

                await ucviMongoRepository.InsertOneAsync(ucviModel);

                logger.LogTraceAndDebug($"InsertUvciAsync finished");

                return true;
            }

            logger.LogTraceAndDebug($"InsertUvciAsync finished");

            return false;
        }
    }
}
