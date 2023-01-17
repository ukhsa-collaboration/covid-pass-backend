using System.Collections.Generic;
using CovidCertificate.Backend.Models.Enums;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration
{
    public class EligibilityCondition
    {
        public string NameOfProduct { get; private set; }
        public IEnumerable<string> SnomedCodes { get; private set; }
        public List<List<string>> VaccineCombinations { get; private set; }
        public DataType ProductType { get; private set; }
        public int EligibilityPeriodHours { get; private set; }
        public int? MostRecentResultMaxHoursAgo { get; private set; }
        public TimeFormat FormatValidity { get; private set; }
        public int? MaximumHoursBetweenResults { get; private set; }
        public int? MinimumHoursBetweenResults { get; private set; }
        public int MinCount { get; private set; }
        public int ResultValidAfterHoursFromLastResult { get; set; }
        public ResultStatus? Result { get; private set; }
        public Eligibility MakesEligibleOrIneligible { get; private set; }
        public IEnumerable<EligibilityNotFollowedBy> NotFollowedBy { get; private set; }
        public Dictionary<string, IEnumerable<string>> AllowedCountries { get; private set; }
        public int? ErrorCode { get; private set; }


        [JsonConstructor]
        public EligibilityCondition(string nameOfProduct, IEnumerable<string> snomedCodes, List<List<string>> vaccineCombinations, DataType productType, int eligibilityPeriodHours, int? mostRecentResultMaxHoursAgo, TimeFormat formatValidity, int? maximumHoursBetweenResults, int? minimumHoursBetweenResults, int minCount, int resultValidAfterHoursFromLastResult, ResultStatus? result, Eligibility makesEligibleOrIneligible, IEnumerable<EligibilityNotFollowedBy> notFollowedBy, Dictionary<string, IEnumerable<string>> allowedCountries, int? errorCode) {
            NameOfProduct = nameOfProduct;
            SnomedCodes = snomedCodes;
            VaccineCombinations = vaccineCombinations;
            ProductType = productType;
            EligibilityPeriodHours = eligibilityPeriodHours;
            MostRecentResultMaxHoursAgo = mostRecentResultMaxHoursAgo;
            FormatValidity = formatValidity;
            MaximumHoursBetweenResults = maximumHoursBetweenResults;
            MinimumHoursBetweenResults = minimumHoursBetweenResults;
            MinCount = minCount;
            ResultValidAfterHoursFromLastResult = resultValidAfterHoursFromLastResult;
            Result = result;
            MakesEligibleOrIneligible = makesEligibleOrIneligible;
            NotFollowedBy = notFollowedBy;
            AllowedCountries = allowedCountries;
            ErrorCode = errorCode;
        }

        public static EligibilityCondition Copy(EligibilityCondition eligibilityCondition)
        {

            return new EligibilityCondition(
                eligibilityCondition.NameOfProduct,
                eligibilityCondition.SnomedCodes,
                eligibilityCondition.VaccineCombinations,
                eligibilityCondition.ProductType,
                eligibilityCondition.EligibilityPeriodHours,
                eligibilityCondition.MostRecentResultMaxHoursAgo,
                eligibilityCondition.FormatValidity,
                eligibilityCondition.MaximumHoursBetweenResults,
                eligibilityCondition.MinimumHoursBetweenResults,
                eligibilityCondition.MinCount,
                eligibilityCondition.ResultValidAfterHoursFromLastResult,
                eligibilityCondition.Result,
                eligibilityCondition.MakesEligibleOrIneligible,
                eligibilityCondition.NotFollowedBy,
                eligibilityCondition.AllowedCountries,
                eligibilityCondition.ErrorCode
                );
        }
        public EligibilityCondition(DataType productType, int resultValidHours)
        {
            this.ProductType = productType;
            this.ResultValidAfterHoursFromLastResult = resultValidHours;
        }
        
    }
}
