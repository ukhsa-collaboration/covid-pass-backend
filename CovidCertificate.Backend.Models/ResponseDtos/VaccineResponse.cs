using System;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Models.ResponseDtos
{
    public class VaccineResponse
    {
        public VaccineResponse(Vaccine vaccine, TimeZoneInfo timeZone)
        {
            DoseNumber = vaccine.DoseNumber;
            VaccinationDate = TimeZoneInfo.ConvertTimeFromUtc(vaccine.VaccinationDate, timeZone).ToString("yyyy-MM-dd");
            VaccineManufacturer = vaccine.VaccineManufacturer;
            DiseaseTargeted = vaccine.DiseaseTargeted;
            VaccineType = vaccine.VaccineType;
            Product = vaccine.Product;
            VaccineBatchNumber = vaccine.VaccineBatchNumber;
            CountryOfVaccination = vaccine.CountryOfVaccination;
            Authority = vaccine.Authority;
            Site = vaccine.Site;
            DisplayName = vaccine.DisplayName;
            TotalSeriesOfDoses = vaccine.TotalSeriesOfDoses;
            IsBooster = vaccine.IsBooster;
            ValidityType = vaccine.ValidityType;
        }
        public int DoseNumber { get; }
        public string VaccinationDate { get; }
        public Tuple<string, string> VaccineManufacturer { get; }
        public Tuple<string, string> DiseaseTargeted { get; }
        public Tuple<string, string> VaccineType { get; }
        public Tuple<string, string> Product { get; }
        public string VaccineBatchNumber { get; }
        public string CountryOfVaccination { get; }
        public string Authority { get; }
        public string Site { get; }
        public string DateTimeOfTest => VaccinationDate;
        public string ValidityType { get; }
        public string DisplayName { get; }
        public int TotalSeriesOfDoses { get; }
        public bool IsBooster { get; }
    }
}
