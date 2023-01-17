using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Utils.Timing
{
    public static class TimeMeasurer
    {
        /// <summary>
        /// Run and measure execution time for an asynchronous Function that returns a Task&lt;TReturn&gt; object
        /// </summary>
        /// <param name="function"></param>
        /// <returns>A TimeMeasurerResult containing the execution time and return value</returns>
        public static async Task<TimeMeasurerResult<TReturn>> StartFunctionAsync<TReturn>(Func<Task<TReturn>> function)
        {
            var stopwatch = Stopwatch.StartNew();
            var res = await function();
            stopwatch.Stop();
            var executionTime = stopwatch.Elapsed;

            return new TimeMeasurerResult<TReturn>(executionTime, res);
        }

        /// <summary>
        /// Run and measure execution time for a synchronous Function that returns a TReturn object
        /// </summary>
        /// <param name="function"></param>
        /// <returns>A TimeMeasurerResult containing the execution time and return value</returns>
        public static TimeMeasurerResult<TReturn> StartFunction<TReturn>(Func<TReturn> function)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = function();
            stopwatch.Stop();
            var executionTime = stopwatch.Elapsed;

            return new TimeMeasurerResult<TReturn>(executionTime, result);
        }

        /// <summary>
        /// Run and measure execution time for an asynchronous Action that returns a Task&lt;Action&gt; object
        /// </summary>
        /// <param name="function"></param>
        /// <returns>A TimeMeasurerResult containing the execution time</returns>
        public static async Task<TimeMeasurerResult> StartActionAsync(Func<Task> function)
        {
            var stopwatch = Stopwatch.StartNew();
            await function();
            stopwatch.Stop();
            var executionTime = stopwatch.Elapsed;

            return new TimeMeasurerResult(executionTime);
        }

        /// <summary>
        /// Run and measure execution time for a synchronous Action that returns an Action object
        /// </summary>
        /// <param name="function"></param>
        /// <returns>A TimeMeasurerResult containing the execution time</returns>
        public static TimeMeasurerResult StartAction(Action function)
        {
            var stopwatch = Stopwatch.StartNew();
            function();
            stopwatch.Stop();
            var executionTime = stopwatch.Elapsed;

            return new TimeMeasurerResult(executionTime);
        }
    }
}
