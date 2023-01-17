using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class QRCodeLayer
    {
        [JsonProperty("1")]
        public string barcodeIssuerCountry { get; set; }

        [JsonProperty("6")]
        public long DateOfIssue { get; set; }

        [JsonProperty("4")]
        public long DateOfExpiry { get; set; }

        [JsonProperty("-260")]
        public ContentLayer content { get; set; }

        public QRCodeLayer()
        {
            content = new ContentLayer();
        }
    }
    public class ContentLayer
    {
        [JsonProperty("1")]
        public DomesticQRCode code { get; set; }

    }



    public class DomesticQRCode
    {
        [JsonProperty("d")]
        public List<CertModel> Certificates { get; set; }

        [JsonProperty("dob")]
        public string DateOfBirth { get; set; }

        [JsonProperty("nam")]
        public Name Name { get; set; }

        [JsonProperty("ver")]
        public string Version { get; set; }

        public DomesticQRCode()
        {

        }
    }

    public class CertModel
    {
        [JsonProperty("ci")]
        public string UVCI { get; set; }

        [JsonProperty("co")]
        public string Country { get; set; }

        [JsonProperty("is")]
        public string Issuer { get; set; }

        [JsonProperty("df")]
        public DateTime DateFrom { get; set; }

        [JsonProperty("du")]
        public DateTime DateUntil { get; set; }

        [JsonProperty("pm")]
        public int CertificateType { get; set; }

        [JsonProperty("po")]
        public string[] Policy { get; set; }
    }

    public class Name
    {
        [JsonProperty("fn")]
        public string Surname { get; set; }

        [JsonProperty("gn")]
        public string Forename { get; set; }

        [JsonProperty("fnt")]
        public string SurnameStandardised { get; set; }

        [JsonProperty("gnt")]
        public string ForenameStandardised { get; set; }

    }

    public class DomesticQRValues
    {
        public string Version { get; set; }
        public string Created { get; set; }
        public string Issuer { get; set; }
        public int CertificateType { get; set; }
    }

    public class DomesticPolicy
    {
        public string UKMandatory { get; set; }
        public string UKVoluntary { get; set; }
        public string CYVoluntary { get; set; }
        public string PlanB { get; set; }
    }
}
