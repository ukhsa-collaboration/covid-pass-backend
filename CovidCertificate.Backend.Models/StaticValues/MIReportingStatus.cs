namespace CovidCertificate.Backend.Models.StaticValues
{
    public static class MIReportingStatus
    {
        public const string Success = "SUCCESS";
        public const string SuccessCert = "SUCCESS-CERT";
        public const string SuccessNoCert = "SUCCESS-NOCERT";
        public const string FailureBadRequest = "FAILURE-BADREQUEST";
        public const string FailureUnauth = "FAILURE-UNAUTH";
        public const string Failure = "FAILURE";
        public const string FailureForbidden = "FAILURE-FORBIDDEN";
        public const string FailureInternal = "FAILURE-INTERNAL";
        public const string FailureInvalid = "FAILURE-INVALID";
        public const string FailureTooManyRequests = "FAILURE-TOOMANYREQUESTS";
        public const string FailureNoContent = "FAILURE-NO-CONTENT";
        public const string FailureNoPdfBody = "FAILURE-NOPDFBODY";
        public const string FaliureDisabled = "FALIURE-DISABLED";
    }
}
