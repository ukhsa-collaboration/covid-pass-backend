using System.Collections.Generic;
using CovidCertificate.Backend.Utils.Extensions;

namespace CovidCertificate.Backend.Models.DataModels.PassData
{
    public class VaccinePassData
    {
        public string VaccinationDate { get; set; }
        public string Product { get; set; }
        public string Manufacturer { get; set; }
        public string VaccineType { get; set; }
        public string BatchNumber { get; set; }
        public string DiseaseTargeted { get; set; }
        public string CountryOfVaccination { get; set; }
        public string Authority { get; set; }
        public string AdministeringCentre { get; set; }
        public string Uvci { get; set; }
        public string Dose { get; set; }

        public VaccinePassData(Vaccine vaccine, Dictionary<string, string> passLabels, string languageCode)
        {
            VaccinationDate = StringUtils.GetTranslatedAndFormattedDate(vaccine.VaccinationDate, languageCode);
            Product = vaccine.Product.Item2;
            Manufacturer = vaccine.VaccineManufacturer.Item2;
            VaccineType = vaccine.VaccineType.Item2;
            BatchNumber = vaccine.VaccineBatchNumber;
            DiseaseTargeted = vaccine.DiseaseTargeted.Item2;
            CountryOfVaccination = vaccine.CountryOfVaccination;
            Authority = vaccine.Authority;
            AdministeringCentre = vaccine.CountryOfVaccination == "GB" ? vaccine.Site : string.Empty;
            Dose = $"{vaccine.DoseNumber} {passLabels["of"]} {vaccine.TotalSeriesOfDoses}";
        }
    }
}
