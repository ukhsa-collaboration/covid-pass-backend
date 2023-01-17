using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Utils
{
    public static class HttpRetryPolicyUtils
    {
        public static AsyncPolicyWrap<HttpResponseMessage> CreateRetryPolicyWrapCustomResponseCodes(int retryCount, int retrySleepDuration, int timeout, string errorMessage, ILogger logger, List<HttpStatusCode> statusCodes)
        {
            var retryPolicy = Policy.Handle<HttpRequestException>()
              .OrResult<HttpResponseMessage>(r => statusCodes.Contains(r.StatusCode))
              .Or<TimeoutRejectedException>()
              .Or<TaskCanceledException>()
              .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(retrySleepDuration / 1000, retryAttempt)),
                (response, _, retries, context) => logger.LogWarning($"Error {errorMessage} on attempt no. {retries} out of {retryCount}.{(retries != retryCount ? $" Retrying in {Math.Pow(retrySleepDuration / 1000, retries)}s." : "")} Error message: {response.Exception?.Message ?? response.Result.ReasonPhrase}"));

            var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(timeout), TimeoutStrategy.Pessimistic);

            return retryPolicy.WrapAsync(timeoutPolicy);
        }

        public static AsyncPolicyWrap CreateGenericRetryPolicy(int retryCount, int retrySleepDuration, int timeout, string errorMessage, ILogger logger)
        {
            var retryPolicy = Policy.Handle<Exception>()
              .WaitAndRetryAsync(retryCount, iretryAttempt => TimeSpan.FromSeconds(Math.Pow(retrySleepDuration, iretryAttempt)), (exception, _, retries, context) => logger.LogWarning($"Error {errorMessage} on attempt no. {retries} out of {retryCount}.{(retries != retryCount ? $" Retrying in {retrySleepDuration}ms." : "")} Error message: {exception.Message}"));
            var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(timeout), TimeoutStrategy.Pessimistic);

            return retryPolicy.WrapAsync(timeoutPolicy);
        }
    }
}
