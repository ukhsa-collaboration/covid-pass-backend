namespace CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration
{
    public class IneligibilityPeriod
    {
        public int LockoutPeriodDays { get; set; }
        public int NegationTestPeriodDays { get; set; }
        public int StackingPeriodDays { get; set; }

        public IneligibilityPeriod(int lockoutPeriodDays, int negationTestPeriodDays, int stackingPeriodDays)
        {
            LockoutPeriodDays = lockoutPeriodDays;
            NegationTestPeriodDays = negationTestPeriodDays;
            StackingPeriodDays = stackingPeriodDays;
        }
    }
}
