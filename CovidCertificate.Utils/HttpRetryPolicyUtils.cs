using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
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
                    (response, _, retries, context) => logger.LogWarning(
                        $"Error {errorMessage} on attempt no. {retries} out of {retryCount}.{(retries != retryCount ? $" Retrying in {Math.Pow(retrySleepDuration / 1000, retries)}s." : "")} Error message: '{response.Exception?.Message ?? response.Result.ReasonPhrase}', inner ex. '{GetInnerExceptionsMessages(response.Exception)}'."));

            var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(timeout), TimeoutStrategy.Pessimistic);

            return retryPolicy.WrapAsync(timeoutPolicy);
        }

        public static AsyncPolicyWrap CreateGenericRetryPolicy(int retryCount, int retrySleepDuration, int timeout, string errorMessage, ILogger logger)
        {
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(retryCount,
                    iretryAttempt => TimeSpan.FromSeconds(Math.Pow(retrySleepDuration, iretryAttempt)),
                    (exception, _, retries, context) => logger.LogWarning(
                        $"Error '{errorMessage}' on attempt no. {retries} out of {retryCount}.{(retries != retryCount ? $" Retrying in {retrySleepDuration}ms." : "")} Error message: '{exception.Message}', inner ex. '{GetInnerExceptionsMessages(exception)}'."));
            var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(timeout), TimeoutStrategy.Pessimistic);

            return retryPolicy.WrapAsync(timeoutPolicy);
        }

        private static string GetInnerExceptionsMessages(Exception ex)
        {
            var currentEx = ex;
            var sb = new StringBuilder();
            var count = 0;

            while (currentEx?.InnerException is not null)
            {
                sb.Append($"inner ex. [{count}] message: '{currentEx.InnerException.Message}'. ");
                count++;
                currentEx = currentEx.InnerException;
            }

            return sb.ToString();
        }
    }
}
