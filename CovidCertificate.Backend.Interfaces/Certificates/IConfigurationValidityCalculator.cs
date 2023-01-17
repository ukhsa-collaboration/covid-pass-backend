using CovidCertificate.Backend.Models.DataModels;
using System.Collections.Generic;
using System;
using CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Interfaces.Certificates
{
    public interface IConfigurationValidityCalculator
    {
        /// <summary>
        /// Generate a potential Certificate from an enumerable of user results (currently diagnostic,
        ///    vaccination) and a configuration
        /// </summary>
        /// <returns>
        /// The first Certificate able to be generated based on the Configuration, otherwise null
        /// </returns>
        /// <param name="allTestResults">An enumerable of all diagnostic and vaccination models.</param>
        /// <param name="configuration">The configuration to use for generating a certificate</param>
        IEnumerable<Certificate> GenerateCertificatesUsingRules(IEnumerable<IGenericResult> allTestResults, IEnumerable<EligibilityRules> rules, CovidPassportUser user, DateTime effectiveDateTime);
    }
}
