using System;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace CovidCertificate.Backend.Models 
{
    [OpenApiExample(typeof(SendCertificateRequestModelExample))]
    public class SendCertificateRequestModel
    {
        public string email;
        public SendCertificateRequestModel(string email)
        {
            this.email = email;
        }
    }   
}


