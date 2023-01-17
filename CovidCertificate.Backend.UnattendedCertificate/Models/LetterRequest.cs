using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.UnattendedCertificate.Models
{
    public class LetterRequest
    {
        public string NhsNumber { get; set; } 
        public string FirstName { get; set; } 
        public string LastName { get; set; } 
        public string DateOfBirth { get; set; } 
        public string CorrelationId { get; set; }
        public string EmailAddress { get; set; } 
        public string MobileNumber { get; set; }
        public ContactMethodEnum ContactMethod { get; set; }
    }
}
