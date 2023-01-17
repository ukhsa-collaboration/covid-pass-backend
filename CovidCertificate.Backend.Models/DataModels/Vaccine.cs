using FluentValidation;
using System;
using CovidCertificate.Backend.Models.Interfaces;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class Vaccine : IGenericResult
    {
        //Result should never be set, here to implement interface
        public string Result { get; } = "";
        public int DoseNumber { get; set; }
        public DateTime VaccinationDate { get; set; }
        public Tuple<string, string> VaccineManufacturer { get; set; }
        public Tuple<string, string> DiseaseTargeted { get; set; }
        public Tuple<string, string> VaccineType { get; set; }
        public Tuple<string, string> Product { get; set; }
        public string VaccineBatchNumber { get; set; }
        public string CountryOfVaccination { get; set; }
        public string Authority { get; set; }
        public string Site { get; set; }
        public DateTime DateTimeOfTest => VaccinationDate;
        public string ValidityType { get; set; }
        public int TotalSeriesOfDoses { get; set; }
        public string DisplayName { get; set; }
        public string SnomedCode { get; set; }
        public DateTime DateEntered { get; set; }
        public string CountryCode => CountryOfVaccination;
        public string ProcedureCode { get; set; }
        public bool IsBooster { get; set; }

        public Vaccine(DateTime dateTimeOfTest, string countryCode, string validityType)
        {
            VaccinationDate = dateTimeOfTest;
            CountryOfVaccination = countryCode;
            ValidityType = validityType;
        }

        [JsonConstructor]
        public Vaccine(int doseNumber, DateTime vaccinationDate, Tuple<string, string> vaccineManufacturer, Tuple<string, string> diseaseTargeted, Tuple<string, string> vaccineType, Tuple<string, string> product, string vaccineBatchNumber, string countryOfVaccination, string authority, int totalSeriesOfDoses, string site, string displayName, string snomedCode, DateTime dateEntered, string procedureCode, bool isBooster, string validityType)
        {
            DoseNumber = doseNumber;
            VaccinationDate = vaccinationDate;
            VaccineManufacturer = vaccineManufacturer;
            DiseaseTargeted = diseaseTargeted;
            VaccineType = vaccineType;
            Product = product;
            VaccineBatchNumber = vaccineBatchNumber;
            CountryOfVaccination = countryOfVaccination;
            Authority = authority;
            TotalSeriesOfDoses = totalSeriesOfDoses;
            Site = site;
            DisplayName = displayName;
            SnomedCode = snomedCode;
            DateEntered = dateEntered;
            ProcedureCode = procedureCode;
            IsBooster = isBooster;
            ValidityType = validityType;
        }

        public Vaccine() { }
        public Vaccine(Vaccine v)
        {
            DoseNumber = v.DoseNumber;
            VaccinationDate = v.VaccinationDate;
            VaccineManufacturer = v.VaccineManufacturer;
            DiseaseTargeted = v.DiseaseTargeted;
            VaccineType = v.VaccineType;
            Product = v.Product;
            VaccineBatchNumber = v.VaccineBatchNumber;
            CountryOfVaccination = v.CountryOfVaccination;
            Authority = v.Authority;
            TotalSeriesOfDoses = v.TotalSeriesOfDoses;
            Site = v.Site;
            DisplayName = v.DisplayName;
            SnomedCode = v.SnomedCode;
            ProcedureCode = v.ProcedureCode;
            IsBooster = v.IsBooster;
            ValidityType = v.ValidityType;
        }

        public void ValidateObjectAndThrowOnFailures()
        {
            new VaccineValidator().ValidateAndThrow(this);
        }

        private class VaccineValidator : AbstractValidator<Vaccine>
        {
            public VaccineValidator()
            {
                #region date Rules
                RuleFor(x => x.DateTimeOfTest).GreaterThan(new DateTime(2020, 1, 1)).WithMessage("The date of vaccination cannot be before the year 2020.");

                RuleFor(x => x.DateTimeOfTest).LessThanOrEqualTo(DateTime.UtcNow).WithMessage("The date of vaccination cannot be in the future.");
                #endregion
                #region product Rules
                RuleFor(x => x.VaccineManufacturer).NotEmpty().WithMessage("The vaccination product must be specified.");
                #endregion
            }
        }
    }
}
