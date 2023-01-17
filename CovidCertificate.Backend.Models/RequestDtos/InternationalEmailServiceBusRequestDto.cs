using System.Collections.Generic;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.Models.RequestDtos
{
    public class InternationalEmailServiceBusRequestDto
    {
        public string EmailToSendTo { get; set; }
        public CovidPassportUser CovidPassportUser { get; set; }
        public Certificate VaccinationCertificate { get; set; }
        public Certificate RecoveryCertificate { get; set; }
        public IEnumerable<Vaccine> VaccinationData { get; set; }
        public IEnumerable<TestResultNhs> RecoveryData { get; set; }
        public string LanguageCode { get; set; }
        public PDFType Type { get; set; }
        public int DoseNumber { get; set; }
    }
}
