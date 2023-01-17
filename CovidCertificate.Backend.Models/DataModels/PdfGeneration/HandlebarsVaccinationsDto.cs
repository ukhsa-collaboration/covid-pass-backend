namespace CovidCertificate.Backend.Models.DataModels.PdfGeneration
{
    public class HandlebarsVaccinationsDto
    {
        public string Name { get; set; }
        public string DateOfBirth { get; set; }
        public HandlebarsVaccinationDto Vaccination { get; set; }
        public bool VaccinationHeaderOverflows { get; set; }
        public string PageNumber { get; set; }
        public string TotalNumberOfPages { get; set; }
    }

    public class HandlebarsVaccinationDto
    {
        public string DisplayName { get; set; }
        public string QRCodeToken { get; set; }
        public string ExpiryDate { get; set; }
        public string DoseNumber { get; set; }
        public string TotalSeriesOfDoses { get; set; }
        public string Date { get; set; }
        public string Product { get; set; }
        public string Manufacturer { get; set; }
        public string Type { get; set; }
        public string BatchNumber { get; set; }
        public string DiseaseTargeted { get; set; }
        public string CountryOfVaccination { get; set; }
        public string Authority { get; set; }
        public string Site { get; set; }
        public bool IsBooster { get; set; }
    }
}
