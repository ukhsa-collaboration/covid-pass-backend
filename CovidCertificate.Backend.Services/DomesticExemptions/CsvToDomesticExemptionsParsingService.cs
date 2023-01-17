using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CovidCertificate.Backend.Interfaces.DomesticExemptions;
using CovidCertificate.Backend.Models.RequestDtos;
using CsvHelper;
using CsvHelper.Configuration;

namespace CovidCertificate.Backend.Services.DomesticExemptions
{
    public class CsvToDomesticExemptionsParsingService : ICsvToDomesticExemptionsParsingService
    {
        public (List<DomesticExemptionDto> parsedExemptions, List<string> failedRows) ParseCsvToDomesticExemptions(string csvFileContent)
        {
            List<DomesticExemptionDto> successfulRows = new List<DomesticExemptionDto>();
            List<string> failedRows = new List<string>();

            bool isRecordBad = false;

            var csvConfiguration = CreateCsvConfiguration(CultureInfo.InvariantCulture,
                CsvMode.Escape, false,
                null,
                badData =>
                {
                    isRecordBad = true;
                    failedRows.Add(badData.RawRecord);
                });

            using var csv = new CsvReader(new StringReader(csvFileContent), csvConfiguration);

            while (csv.Read())
            {
                if (isRecordBad)
                {
                    isRecordBad = false;
                    continue;
                }

                var record = csv.GetRecord<DomesticExemptionDto>();

                successfulRows.Add(record);
                isRecordBad = false;
            }

            return (successfulRows, failedRows);
        }

        private static CsvConfiguration CreateCsvConfiguration(CultureInfo cultureInfo,
            CsvMode mode,
            bool hasHeaderRecord,
            MissingFieldFound missingFieldFound,
            BadDataFound badDataFound)
        {
            return new CsvConfiguration(cultureInfo)
            {
                Mode = mode,
                HasHeaderRecord = hasHeaderRecord,
                BadDataFound = badDataFound,
                MissingFieldFound = missingFieldFound
            };
        }
    }
}
