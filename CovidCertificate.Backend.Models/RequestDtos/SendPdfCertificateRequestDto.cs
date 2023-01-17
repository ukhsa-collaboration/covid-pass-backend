using CovidCertificate.Backend.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace CovidCertificate.Backend.Models.RequestDtos
{
    public class SendPdfCertificateRequestDto : IEmailModel
    {
        public string EmailAddress { get; set; }
        public string Name { get; set; }
        public Newtonsoft.Json.Linq.JObject DocumentContent { get; set; }
        public string LangCode { get; set; }

        public Dictionary<string, dynamic> GetPersonalisation()
        {
            return new Dictionary<string, dynamic>
            {
                { "application_date", DateTime.UtcNow.ToString("yyyy-MM-dd") },
                { "link_to_file", DocumentContent }            
            };
        }
    }
}
