using System;
using System.Collections.Generic;
using System.Linq;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.NhsApiIntegration.Services
{
    public class MedicalExemptionParser : IMedicalExemptionDataParser
    {
        public IEnumerable<MedicalExemption> Parse(Bundle bundle)
        {
            var exemptions = new List<MedicalExemption>();

            foreach(var questionnaireResponse in bundle.Entry.Where(x => x?.Resource is QuestionnaireResponse)
                                                             .Select(y => y.Resource as QuestionnaireResponse))
            {
                if (questionnaireResponse == null)
                {
                    continue;
                }

                var patient = questionnaireResponse?.Contained?.First() as Patient;
                var nhsNumberIdenitifer = questionnaireResponse?.Subject?.Identifier?.Value;
                var exemptionStatus = (questionnaireResponse?.Item?.FirstOrDefault(x => x.LinkId == "exemptionStatus")?.Answer?.FirstOrDefault()?.Value as Coding)?.Display;
                ExemptionReasonCode exemptionReasonCode = (ExemptionReasonCode)Enum.Parse(typeof(ExemptionReasonCode), (questionnaireResponse?.Item?.FirstOrDefault(x => x.LinkId == "exemptionStatus")?.Answer?.FirstOrDefault()?.Value as Coding)?.Code);
                var exemptionPeriodStartAnswer = questionnaireResponse?.Item
                    ?.FirstOrDefault(x => x.LinkId == "exemptionPeriodStart")?.Answer?.FirstOrDefault()?.Value?.ToString();
                var exemptionPeriodStart = DateTime.TryParse(exemptionPeriodStartAnswer, out var NullableExemptionPeriodStartAnswer) ? (DateTime?)NullableExemptionPeriodStartAnswer : null;
                var exemptionPeriodEndAnswer = questionnaireResponse?.Item
                    ?.FirstOrDefault(x => x.LinkId == "exemptionPeriodEnd")?.Answer?.FirstOrDefault()?.Value?.ToString();
                var exemptionPeriodEnd = DateTime.TryParse(exemptionPeriodEndAnswer, out var NullableExemptionPeriodEndAnswer) ? (DateTime?)NullableExemptionPeriodEndAnswer : null;
                var birthDate = DateTime.Parse(patient.BirthDate);

                exemptions.Add(new MedicalExemption(exemptionStatus, exemptionReasonCode, exemptionPeriodStart, exemptionPeriodEnd));
            }

            return exemptions;
        }
    }
}
