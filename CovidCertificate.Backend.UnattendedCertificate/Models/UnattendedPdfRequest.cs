using System;
using System.Collections.Generic;
using System.Text;
using CovidCertificate.Backend.Models.Deserializers;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.UnattendedCertificate.Models
{
    public class UnattendedPdfRequest
    {
        public string FHIRPatient;
        public string EmailToSendTo;
        public string CorrelationId;
        public int ContactMethodSettable;
        public string MobileNumber;
    }
}
