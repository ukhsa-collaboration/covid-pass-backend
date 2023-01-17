using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Services
{
    public class TwoFactorAuthenticatorService : ITwoFactorAuthenticatorService
    {
        private readonly ITableStorageService tableStorageService;
        private readonly ILogger<TwoFactorAuthenticatorService> logger;
        private readonly ITableNameGenerator tableNameGenerator;

        public TwoFactorAuthenticatorService(
            ITableStorageService tableStorageService,
            TwoFactorSettings twoFactorSettings,
            ILogger<TwoFactorAuthenticatorService> logger,
            ITableNameGenerator tableNameGenerator)
        {
            this.tableStorageService = tableStorageService;
            this.logger = logger;
            this.tableNameGenerator = tableNameGenerator;
        }

        public async Task<TwoFactorAuth> CreateCode(string destination)
        {
            var code = StringUtils.RandomString(6);
            var authCode = new TwoFactorAuth(code, destination);
            try
            {
                var hasAddedToTable = await tableStorageService.AddToTable<TwoFactorAuth>(authCode.GetTableKey(), authCode);
                if (!hasAddedToTable)
                    return default;

                return authCode;
            }
            catch (StorageException e)
            {
                logger.LogCritical(e, e.Message);
                return default;
            }
            catch (Exception e)
            {
                logger.LogCritical(e, e.Message);
                throw;
            }
        }

        public async Task<bool> ValidateCode(TwoFactorAuthValidationDto validationDto)
        {
            //Generate key of potential DTO
            var dtoTableKey = validationDto.GetTableKey();
            var tableName = tableNameGenerator.GetTableName();

            var codeExists = await FindAndDelete(tableName, dtoTableKey);
            if (codeExists)
                return true;

            var yesterdayName = tableNameGenerator.GetTableName(DateTime.UtcNow.AddHours(-1));
            //If not in current table check previous one (1 hour earlier)
            codeExists = await FindAndDelete(yesterdayName, dtoTableKey);
            return codeExists;
        }

        private async Task<bool> FindAndDelete(string tableName, string tableKey)
        {
            var inTable = await tableStorageService.FetchFromTable<TwoFactorAuth>(tableName, tableKey);
            if (inTable == default)
                return false;

            await tableStorageService.RemoveFromStorage<TwoFactorAuthValidationDto>(tableName, tableKey);
            return true;
        }
    }
}
