using System.Collections.Generic;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class Barcode
    {
        public string alternateText { get; set; }
        public string type { get; set; }
        public string value { get; set; }
    }

    public class SourceUri
    {
        public string description { get; set; }
        public string uri { get; set; }
    }

    public class Logo
    {
        public SourceUri sourceUri { get; set; }
    }

    public class PatientDetails
    {
        public string dateOfBirth { get; set; }
        public string dateOfBirthLabel { get; set; }
        public string identityAssuranceLevel { get; set; }
        public string patientId { get; set; }
        public string patientName { get; set; } //required
        public string patientNameLabel { get; set; }
        public string patientIdLabel { get; set; }
    }

    public class VaccinationRecord
    {
        public string code { get; set; }
        public string contactInfo { get; set; }
        public string description { get; set; }
        public string doseDateTime { get; set; } //required
        public string doseLabel { get; set; } //required
        public string lotNumber { get; set; }
        public string manufacturer { get; set; }
        public string manufacturerLabel { get; set; }
        public string provider { get; set; }
        public string providerLabel { get; set; }
    }

    public class TestingRecord
    {
        public string testDescription { get; set; }
        public string administrationDateTime { get; set; } //required
        public string reportDateTime { get; set; }
        public string testCode { get; set; }
        public string testCodeLabel { get; set; }
        public string testPlatform { get; set; }
        public string testPlatformLabel { get; set; }
        public string testResultCode { get; set; }
        public string provider { get; set; }
        public string providerLabel { get; set; }
        public string testResultDescription { get; set; }
        public string contactInfo { get; set; }
        public string specimen { get; set; }
        public string specimenLabel { get; set; }
    }

    public class VaccinationDetails
    {
        public List<VaccinationRecord> vaccinationRecord { get; set; }
    }

    public class TestingDetails
    {
        public List<TestingRecord> testingRecord { get; set; }
    }

    public class CovidCardObject
    {
        public string id { get; set; } //required
        public string issuerId { get; set; } //required
        public Barcode barcode { get; set; }
        public string cardColorHex { get; set; }
        public string expiration { get; set; }
        public string expirationLabel { get; set; }
        public Logo logo { get; set; }
        public PatientDetails patientDetails { get; set; } //required
        public string title { get; set; } //required
        public VaccinationDetails vaccinationDetails { get; set; }
        public TestingDetails testingDetails { get; set; }
        public string summary { get; set; }
        public string cardDescription { get; set; }
        public string cardDescriptionLabel { get; set; }
        public SecurityAnimation securityAnimation { get; set; }
    }

    public class Payload
    {
        public List<CovidCardObject> covidCardObjects { get; set; }
    }

    public class SecurityAnimation
    {
        public string animationType { get; set; }
    }
}
