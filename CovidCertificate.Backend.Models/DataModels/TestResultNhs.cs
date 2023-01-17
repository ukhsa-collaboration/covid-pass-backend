using Newtonsoft.Json;
using System;
using System.Text;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class TestResultNhs : IGenericResult
    {

        [JsonConstructor]
        public TestResultNhs(DateTime dateTimeOfTest, string result, string validityType, string processingCode, string testKit, Tuple<string, string> diseaseTargeted, string authority, string countryOfAuthority, bool isNAAT)
        {
            this.DateTimeOfTest = dateTimeOfTest;
            this.Result = result;
            this.ValidityType = validityType;
            this.ProcessingCode = processingCode;
            this.TestKit = testKit;
            this.DiseaseTargeted = diseaseTargeted;
            this.Authority = authority;
            this.CountryOfAuthority = countryOfAuthority;
            this.IsNAAT = isNAAT;
        }
        public TestResultNhs(TestResultNhs t)
        {
            DateTimeOfTest = t.DateTimeOfTest;
            Result = t.Result;
            ValidityType = t.ValidityType;
            ProcessingCode = t.ProcessingCode;
            TestKit = t.TestKit;
            DiseaseTargeted = t.DiseaseTargeted;
            Authority = t.Authority;
            CountryOfAuthority = t.CountryOfAuthority;
            IsNAAT = t.IsNAAT;
        }
        public DateTime DateTimeOfTest { get; }
        public string Result { get; }
        public string ValidityType { get; }
        public string ProcessingCode { get; }
        public string TestKit { get; set; }
        public Tuple<string, string> DiseaseTargeted { get; }
        public string Authority { get; }
        public string CountryOfAuthority { get; }
        public string CountryCode => CountryOfAuthority;
        public string TestLocation { get; set; }
        public string RAT { get; set; }
        public string TestType { get; set; }
        public bool IsNAAT { get; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Authority:").Append(this.Authority??"").AppendLine();
            sb.Append("Result:").Append(this.Result??"").AppendLine();
            sb.Append("DiseaseTargeted:").Append(this.DiseaseTargeted?.ToString()).AppendLine();
            sb.Append("ProcessingCode:").Append(this.ProcessingCode??"").AppendLine();
            sb.Append("TestKit:").Append(this.TestKit??"").AppendLine();
            sb.Append("ValidityType:").Append(this.ValidityType??"").AppendLine();
            sb.Append("CountryOfAuthority:").Append(this.CountryOfAuthority??"").AppendLine();
            sb.Append("DateTimeOfTest:").Append(this.DateTimeOfTest).AppendLine();
            sb.Append("IsNAAT:").Append(this.IsNAAT).AppendLine();

            return sb.ToString();
        }

        public bool IsPositive()
        {
            return string.Equals(Result.ToUpper(), "POSITIVE");
        }

        public bool IsNegative()
        {
            return string.Equals(Result.ToUpper(), "NEGATIVE");
        }
    }
}
