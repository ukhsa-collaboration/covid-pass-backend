using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.Certificates.UVCI
{
    public abstract class UVCIGenerator<T> where T: MongoDocument, IUVCIGeneratorModel
    {
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        protected readonly IConfiguration configuration;
        protected readonly ILogger<UVCIGenerator<T>> logger;
        protected readonly IUVCIRepository<T> uvciRepository;

        protected UVCIGenerator(IConfiguration configuration,
            ILogger<UVCIGenerator<T>> logger,
            IUVCIRepository<T> uvciRepository
        )
        {
            this.configuration = configuration;
            this.logger = logger;
            this.uvciRepository = uvciRepository;
        }

        protected async Task<string> InsertUvciAsync(GenerateAndInsertUvciCommand command, string uvci,
            DateTime generationDateTime)
        {
            var addedToMongo = false;

            while (!addedToMongo)
            {
                addedToMongo = await uvciRepository.InsertUvciAsync(uvci, generationDateTime, command);
            }

            return uvci;
        }

        protected string GenerateUvciString(string uvciIssuer, out DateTime generationDateTime)
        {
            var prefix = configuration["UVCIPrefix"];
            var version = configuration["UVCIVersion"];

            var certificateGenerationTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            generationDateTime = DateTimeOffset.FromUnixTimeMilliseconds(certificateGenerationTime).DateTime;

            var sb = new StringBuilder("");

            var randomString = new string(Enumerable.Repeat(Chars, 8)
                .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());

            var uvciString = sb
                .Append(prefix).Append(":")
                .Append(version).Append(":")
                .Append(uvciIssuer).Append(":")
                .Append(certificateGenerationTime)
                .Append(randomString).ToString();

            char checkSumChar = UvciGeneratorUtils.GenerateCheckCharacter(uvciString);

            return $"{uvciString}#{checkSumChar}";
        }
    }
}
