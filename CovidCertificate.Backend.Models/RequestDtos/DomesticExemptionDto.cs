using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Validators;
using CsvHelper.Configuration.Attributes;
using FluentValidation.Results;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Models.RequestDtos
{
    public class DomesticExemptionDto
    {
        [Index(0)]
        public string NhsNumber { get; set; }

        [Index(1)]
        public DateTime DateOfBirth { get; set; }

        [Index(2)]
        [Optional]
        public string Reason { get; set; }

        public static DomesticExemptionDtoValidator Validator = new DomesticExemptionDtoValidator();

        [JsonConstructor]
        public DomesticExemptionDto(string nhsNumber, DateTime dateOfBirth, string reason)
        {
            NhsNumber = nhsNumber;
            Reason = reason;
            DateOfBirth = dateOfBirth;
        }

        public DomesticExemptionRecord ToDomesticExemption() => new DomesticExemptionRecord(this);

        public DomesticExemptionRecord ToMedicalExemption()
        {
            var exemption = new DomesticExemptionRecord(this);
            exemption.IsMedicalExemption = true;
            return exemption;
        }

        public virtual async Task<ValidationResult> ValidateObjectAsync()
        {
            return await Validator.ValidateAsync(this);
        }

        public override string ToString()
        {
            return NhsNumber + "," + DateOfBirth + "," + Reason;
        }
    }
}