using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.Interfaces;
using FluentValidation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Interfaces.UserInterfaces;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class Certificate : IUserBaseInformation, IJwtTokenData
    {
        public Certificate(string name, DateTime dateOfBirth, DateTime validity, DateTime eligibility, CertificateType certificateType, CertificateScenario certificateScenario, IEnumerable<IGenericResult> eligibilityResults = null)
        {
            Name = name;
            DateOfBirth = dateOfBirth.ToUniversalTime();
            ValidityEndDate = validity.ToUniversalTime();
            eligibilityEndDate = eligibility.ToUniversalTime();
            CertificateType = certificateType;
            CertificateScenario = certificateScenario;
            EligibilityResults = eligibilityResults;
            QrCodeTokens = new List<string>();
        }

        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? ValidityStartDate { get; set; }
        public DateTime ValidityEndDate { get; set; }
        public DateTime eligibilityEndDate { get; set; }
        public CertificateType CertificateType { get; set; }
        public CertificateScenario CertificateScenario { get; set; }
        public List<string> QrCodeTokens { get; set; }
        public string UniqueCertificateIdentifier { get; set; }
        [JsonIgnore]
        public IEnumerable<IGenericResult> EligibilityResults { get; set; }
        public int? PolicyMask { get; set; }
        public string[] Policy { get; set; }
        public string PKICountry { get; set; }
        public string Issuer { get; set; }
        public string Country { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("CertificateType:").Append(this.CertificateType).AppendLine();
            sb.Append("CertificateScenario:").Append(this.CertificateScenario).AppendLine();
            sb.Append("EligibilityEndDate:").Append(this.eligibilityEndDate).AppendLine();
            sb.Append("ValidityStartDate:").Append(this.ValidityEndDate).AppendLine();
            sb.Append("ValidityEndDate:").Append(this.ValidityEndDate).AppendLine();
            sb.Append("PolicyMask:").Append(this.PolicyMask).AppendLine();
            sb.Append("Policy:").Append(this.Policy).AppendLine();
            sb.Append("PKICountry:").Append(this.PKICountry).AppendLine();
            sb.Append("Issuer:").Append(this.Issuer).AppendLine();
            sb.Append("Country:").Append(this.Issuer).AppendLine();

            return sb.ToString();
        }

        public IEnumerable<Claim> GetClaims()
        {
            var claims = new List<Claim>();
            claims.Add(new Claim("name", Name));

            if (DateOfBirth != default)
                claims.Add(new Claim(ClaimTypes.DateOfBirth, DateOfBirth.ToShortDateString()));

            if (ValidityEndDate != default)
                claims.Add(new Claim("exp", ValidityEndDate.ToFileTimeUtc().ToString()));

                claims.Add(new Claim("iat", DateTime.UtcNow.ToFileTimeUtc().ToString()));

            return claims;
        }

        public void ConvertTimeZone(TimeZoneInfo timeZoneInfo)
        {
            //Truncates times to whole minute and converts to universal
            this.ValidityEndDate = TimeFormatConvert.ToUniversal(this.ValidityEndDate);
            this.eligibilityEndDate = TimeFormatConvert.ToUniversal(this.eligibilityEndDate);
        }

        public IEnumerable<Vaccine> GetAllVaccinationsFromEligibleResults()
        {
            return EligibilityResults.Where(x => x is Vaccine).Select(y => (Vaccine)y).OrderByDescending(x => x.DateTimeOfTest);
        }

        public TestResultNhs GetLatestDiagnosticResultFromEligibleResultsOrDefault()
        {
            return EligibilityResults.Where(x => x is TestResultNhs).Select(y => (TestResultNhs)y).OrderByDescending(x => x.DateTimeOfTest).FirstOrDefault();
        }       
    }
}
