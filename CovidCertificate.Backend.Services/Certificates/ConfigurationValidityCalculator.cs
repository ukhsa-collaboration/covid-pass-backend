using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using Microsoft.Extensions.Configuration;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class ConfigurationValidityCalculator : IConfigurationValidityCalculator
    {
        private readonly ILogger<ConfigurationValidityCalculator> logger;
        private readonly IConfiguration configuration;
        private readonly int P5CertificateExpiryInHours;
        private readonly IBoosterValidityService boosterValidityService;

        public ConfigurationValidityCalculator(ILogger<ConfigurationValidityCalculator> logger, IConfiguration configuration, IBoosterValidityService boosterValidityService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.boosterValidityService = boosterValidityService;
            P5CertificateExpiryInHours = int.TryParse(configuration["P5CertificateExpiryInHours"] ?? "72", out var duration) ? duration : 72;
        }

        public IEnumerable<Certificate> GenerateCertificatesUsingRules(IEnumerable<IGenericResult> results, IEnumerable<EligibilityRules> rules, CovidPassportUser user, DateTime effectiveDateTime)
        {
            logger.LogTraceAndDebug($"{nameof(GenerateCertificatesUsingRules)} was invoked");

            var certificates = new List<Certificate>();
            foreach (var rule in rules)
            {
                var potentialCertificate = ReturnCertificateIfResultsSatisfyStatus(results, rule, user, effectiveDateTime);
                if (potentialCertificate != default)
                {
                    potentialCertificate.Policy = rule.Policy;
                    potentialCertificate.PolicyMask = rule.PolicyMask;
                    certificates.Add(potentialCertificate);
                }
            }

            logger.LogTraceAndDebug($"{nameof(GenerateCertificatesUsingRules)} has finished");
            return certificates;
        }

        //If results satisfy conditions within a status then that status can be used to generate a certificate
        private Certificate ReturnCertificateIfResultsSatisfyStatus(IEnumerable<IGenericResult> results, EligibilityRules statuses, CovidPassportUser user, DateTime effectiveDateTime)
        {
            var expiryTime = DateTime.MinValue;
            var eligibilityTime = DateTime.MinValue;
            var eligibilityResults = new List<IGenericResult>();
            //Results must satisfy every condition to generate a Certificate
            foreach (var condition in statuses.Conditions)
            {
                var filteredResults = results;
                if (statuses.ConfigurationName.Contains("booster"))
                {
                    if (!boosterValidityService.IsBoosterWithinCorrectTimeFrame(results))
                    {
                        return null;
                    }
                }
                else if (statuses.Scenario == CertificateScenario.Domestic || statuses.Scenario == CertificateScenario.Isolation)
                {
                    filteredResults = RemoveChildDoses(filteredResults);
                    filteredResults = boosterValidityService.RemoveBoosters(filteredResults);
                }

                var satisfiedCondition = CheckIfResultsSatisfyCondition(filteredResults, condition, statuses.ValidityPeriodHours, user, effectiveDateTime);

                if (ConditionMakesCertificateTypeInvalid(satisfiedCondition?.Expiry, condition.MakesEligibleOrIneligible))
                {
                    return null;
                }

                if (condition.MakesEligibleOrIneligible == Eligibility.Eligible)
                {
                    //There is a previous check for null value
                    if (satisfiedCondition.Expiry > expiryTime)
                        expiryTime = satisfiedCondition.Expiry;
                    if (satisfiedCondition.Eligibility > eligibilityTime)
                        eligibilityTime = satisfiedCondition.Eligibility;
                    eligibilityResults.AddRange(satisfiedCondition.EligibilityResults);
                }
            }
            //This should never be the case as there should always be a condition to be eligible
            if (expiryTime == DateTime.MinValue)
                return null;

            return new Certificate(default, default, expiryTime, eligibilityTime, statuses.CertificateType, statuses.Scenario, eligibilityResults);
        }

        //Check if allTestResults satisfy a given condition. If they do return the expiry time it generates else return null
        private CertificateMetadata CheckIfResultsSatisfyCondition(IEnumerable<IGenericResult> allResults, EligibilityCondition condition, double expiryHours, CovidPassportUser user, DateTime effectiveDateTime)
        {
            //Get all results of the correct type specified in the conditions
            var listOfResultsWithCorrectType = allResults.Where(x => CheckTestResultEqualsCorrectType(x, condition.ProductType));

            //CHECK IF allResults.country is in condition.allowedCountries list
            var listOfResultsWithCorrectCountry = listOfResultsWithCorrectType.Where(x => CheckCountries(x, condition.AllowedCountries, condition.SnomedCodes != null));

            //Get all tests with correct product or SNOMED code based on condition values
            var listOfResultsOfGivenProduct = CheckTestGivenProduct(condition, listOfResultsWithCorrectCountry);

            //Check these results match the result in the condition as well as being within the specified time period
            // then order by most recent first
            var listOfResultsWithCorrectResultAndTime = listOfResultsOfGivenProduct.Where(x => CheckResult(x, condition.Result) &&
                                                                                        x.DateTimeOfTest.AddHours(condition.EligibilityPeriodHours + condition.ResultValidAfterHoursFromLastResult) > effectiveDateTime &&
                                                                                        x.DateTimeOfTest.AddHours(condition.ResultValidAfterHoursFromLastResult) < effectiveDateTime)
                                                                            .OrderByDescending(x => x.DateTimeOfTest);

            var listOfValidResults = ResultsThatSatisfyMaxMinHoursBetweenResults(condition, listOfResultsWithCorrectResultAndTime);

            //No listOfValidResults means no Certificate can be generated off this condition
            if (listOfValidResults.Count() == 0)
                return null;

            //If shouldBeLast is true and the most recent result in listOfValidResults is not the most recent result for that type
            // then no Certificate can be generated
            if (condition.NotFollowedBy.Any())
            {
                listOfValidResults = ValidTestsNotFollowedByInvalidatingTest(listOfValidResults, allResults, condition.NotFollowedBy);
            }

            return listOfValidResults.Count() >= condition.MinCount
                && MostRecentResultMaxHoursAgoMet(listOfValidResults, condition)
                                            ? CalculateExpiryAndEligibility(listOfValidResults, expiryHours, condition.ResultValidAfterHoursFromLastResult, condition.EligibilityPeriodHours, condition.MostRecentResultMaxHoursAgo, user)
                                            : null;
        }

        private bool MostRecentResultMaxHoursAgoMet(IEnumerable<IGenericResult> results, EligibilityCondition condition)
        {
            if(condition.MostRecentResultMaxHoursAgo == null)
            {
                return true;
            }

            var mostRecentResult = results.OrderByDescending(r => r.DateTimeOfTest).First();

            return mostRecentResult.DateTimeOfTest.AddHours(condition.MostRecentResultMaxHoursAgo.Value) >= DateTime.UtcNow;
        }

        private CertificateMetadata CalculateExpiryAndEligibility(IEnumerable<IGenericResult> results, double expiryHours, int ResultValidAfterHours, int ResultValidForHours, int? mostRecentResultMaxHoursAgo, CovidPassportUser user)
        {
            var eligibilityResultsValidFor = results.First().DateTimeOfTest.AddHours(ResultValidAfterHours + ResultValidForHours);
            var eligibilityRecentResultMaxTimeAgo = mostRecentResultMaxHoursAgo != null ? results.OrderByDescending(r => r.DateTimeOfTest).First().DateTimeOfTest.AddHours(mostRecentResultMaxHoursAgo.Value)
                                                                                        : DateTime.MaxValue;
            var eligibility = eligibilityResultsValidFor < eligibilityRecentResultMaxTimeAgo ? eligibilityResultsValidFor
                                                                                             : eligibilityRecentResultMaxTimeAgo;
            var expiry = DateTime.UtcNow.AddHours(expiryHours);
            //If eligibility less then expiry set expiry to eligibility
            if (eligibility < expiry)
            {
                expiry = eligibility;
            }

            if (user.IdentityProofingLevel != IdentityProofingLevel.P9 && expiry > DateTime.UtcNow.AddHours(P5CertificateExpiryInHours)&&user.DomesticAccessLevel != DomesticAccessLevel.U12)
            {
                expiry = DateTime.UtcNow.AddHours(P5CertificateExpiryInHours);
            }
            
            // P5 users within their grace period should only be able to generate certificate lasting as long as their grace period lasts.
            if (user.GracePeriod?.IsActive == true)
            {
                var gracePeriodEndsOn = user.GracePeriod.EndsOn;

                if (gracePeriodEndsOn < expiry)
                {
                    expiry = gracePeriodEndsOn;
                }

                if (gracePeriodEndsOn < eligibility)
                {
                    eligibility = gracePeriodEndsOn;
                }
            }

            return new CertificateMetadata(expiry, eligibility, results);
        }

        private IEnumerable<IGenericResult> ResultsThatSatisfyMaxMinHoursBetweenResults(EligibilityCondition condition, IEnumerable<IGenericResult> results)
        {
            var listOfValidResults = new List<IGenericResult>();
            //Check multiple results are within the time frame specified of each other
            if (condition.MinCount > 1 && (condition.MaximumHoursBetweenResults != null || condition.MinimumHoursBetweenResults != null))
            {
                var maxHours = condition.MaximumHoursBetweenResults ?? default;
                var minHours = condition.MinimumHoursBetweenResults ?? default;
                var resultsList = results.ToList();
                for (var i = 0; i < resultsList.Count() - 1; i++)
                {
                    //Check consecutive results
                    var firstResult = resultsList.ElementAt(i);
                    var secondResult = resultsList.ElementAt(i + 1);

                    //If the results are in the same time frame then they are both valid
                    if (((firstResult.DateTimeOfTest <= secondResult.DateTimeOfTest.AddHours(maxHours)) || maxHours == default)
                        && ((firstResult.DateTimeOfTest >= secondResult.DateTimeOfTest.AddHours(minHours)) || (minHours == default)))
                    {
                        //Check that firstResult wasn't added on the last iteration where it would be secondResult
                        if (listOfValidResults.LastOrDefault() != firstResult)
                            listOfValidResults.Add(firstResult);

                        //First time secondResult has been added so can immediately add in to listOfValidResults
                        listOfValidResults.Add(secondResult);
                    }
                }
            }
            //If no minimum time between results specified, or minCount = 1, then all results valid
            else
            {
                listOfValidResults = results.ToList();
            }
            return listOfValidResults;
        }

        private IEnumerable<IGenericResult> ValidTestsNotFollowedByInvalidatingTest(IEnumerable<IGenericResult> validResults,
                                                                                    IEnumerable<IGenericResult> allResults,
                                                                                    IEnumerable<EligibilityNotFollowedBy> notFollowedByCondition)
        {
            foreach (var notFollowedBy in notFollowedByCondition)
            {
                var resultsOfInvalidatingType = allResults.Where(x => x.ValidityType == notFollowedBy.Name
                                                                 && CheckTestResultEqualsCorrectType(x, notFollowedBy.ProductType)
                                                                 && notFollowedBy.Results.Any(y => CheckResult(x, y.Name)));
                if (resultsOfInvalidatingType.Any())
                {
                    var resultOfInvalidatingType = resultsOfInvalidatingType.OrderByDescending(x => x.DateTimeOfTest).First();
                    validResults = validResults.Where(x => x.DateTimeOfTest > resultOfInvalidatingType.DateTimeOfTest);
                }
            }
            return validResults;
        }

        //Check to see if return was found for a condition and if that makes the user eligible or ineligible
        private bool ConditionMakesCertificateTypeInvalid(DateTime? userSatisfiedCondition, Eligibility eligibility)
        {
            var noReturnAndConditionMakesEligible = userSatisfiedCondition == null && eligibility == Eligibility.Eligible;
            var returnAndConditionMakesIneligible = userSatisfiedCondition != null && eligibility == Eligibility.Ineligible;
            return noReturnAndConditionMakesEligible || returnAndConditionMakesIneligible;
        }

        //Check if result is of the correct type
        private bool CheckTestResultEqualsCorrectType(IGenericResult genericResult, DataType type)
        {
            if (type == DataType.Diagnostic)
                return genericResult.GetType() == typeof(TestResultNhs);
            else if (type == DataType.Vaccination)
                return genericResult.GetType() == typeof(Vaccine);

            return false;
        }

        private IEnumerable<IGenericResult> CheckTestGivenProduct(EligibilityCondition condition, IEnumerable<IGenericResult> listOfResultsWithCorrectType)
        {
            if (!condition.SnomedCodes.NullOrEmpty())
            {
                return listOfResultsWithCorrectType.Where(x => CheckSnomedCode(x, condition.SnomedCodes));
            }
            if (!condition.NameOfProduct.NullOrEmpty())
            {
                return listOfResultsWithCorrectType.Where(x => x.ValidityType.Equals(condition.NameOfProduct, StringComparison.OrdinalIgnoreCase));
            }

            if (condition.VaccineCombinations.NullOrEmpty()) return new List<IGenericResult>();

            var ofResultsWithCorrectType = listOfResultsWithCorrectType.ToList();

            var requiredCombinationLength = condition.VaccineCombinations.First().Count;

            if (ofResultsWithCorrectType.Count() >= requiredCombinationLength)
            {
                for (var i = 0; i < ofResultsWithCorrectType.Count() - (requiredCombinationLength-1); i++)
                {
                    //Get the combination of required length
                    var results = new List<IGenericResult>();
                    for (var j = 0; j < requiredCombinationLength; j++)
                    {
                        results.Add(ofResultsWithCorrectType.ElementAt(i + j));
                    }

                    foreach (var conditionVaccine in condition.VaccineCombinations)
                    {
                        var validResults = 0;

                        //Check if the vacc combination is the same as in the rule
                        for(var k = 0; k < requiredCombinationLength; k++)
                        {
                            if (results.ElementAt(k).ValidityType == conditionVaccine.ElementAt(k))
                                validResults++;
                        }

                        if (validResults == requiredCombinationLength)
                        {
                            return ofResultsWithCorrectType;
                        }
                    }
                }
            }
            return new List<IGenericResult>();

        }

        private bool CheckSnomedCode(IGenericResult result, IEnumerable<string> targetSnomeds)
        {
            if (result is Vaccine vaccineModel)
                return targetSnomeds.Contains(vaccineModel.SnomedCode);
            return false;
        }

        //Check if genericResult.Result matches the required ResultStatus
        private bool CheckResult(IGenericResult genericResult, ResultStatus? result)
        {
            //If no result required then any test result is valid
            if (result == null)
            {
                return true;
            }
            return string.Equals(genericResult.Result, result.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private bool CheckCountries(IGenericResult genericResult, Dictionary<string, IEnumerable<string>> allowedCountries, bool checkSnomedCode)
        {
            if(allowedCountries.NullOrEmpty())
            {
                return true;
            }

            var key = GetAllowedCountriesDictionaryKeyValue(genericResult, checkSnomedCode);

            if (allowedCountries.ContainsKey(key))
            {
                var countries = allowedCountries.GetValueOrDefault(key);
                return !countries.Any() || countries.Contains(genericResult.CountryCode);
            }

            return false;
        }

        private string GetAllowedCountriesDictionaryKeyValue(IGenericResult genericResult, bool checkSnomedCode)
        {
            if (checkSnomedCode && genericResult is Vaccine vaccine && vaccine.SnomedCode != null)
            {
                return vaccine.SnomedCode;
            }
            return genericResult.ValidityType;
        }

        private IEnumerable<IGenericResult> RemoveChildDoses(IEnumerable<IGenericResult> results)
        {
            var childDose = new List<Vaccine>();
            foreach (var result in results)
            {
                if (result is Vaccine vaccine && vaccine.SnomedCode == "40384611000001108")
                {
                    childDose.Add((Vaccine)result);
                }
            }
            return results.Except(childDose);
        }
    }
}
