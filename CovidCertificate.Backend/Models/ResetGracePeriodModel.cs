using System;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace CovidCertificate.Backend.Models
{
    [OpenApiExample(typeof(ResetGracePeriodModelExample))]
    public class ResetGracePeriodModel
    {
        public string nhsNumber;
        public string dateOfBirth;
        public ResetGracePeriodModel(string nhsno, string dob)
        {
            this.nhsNumber = nhsno;
            this.dateOfBirth = dob;
        }
    }

}

