using System;

namespace CovidCertificate.Backend.Utils.Timing
{
    public class TimeMeasurerResult
    {
        public TimeSpan Duration { get; }

        internal TimeMeasurerResult(TimeSpan duration)
        {
            Duration = duration;
        }
    }

    public class TimeMeasurerResult<T> : TimeMeasurerResult
    {
        public T Result { get; }

        internal TimeMeasurerResult(TimeSpan duration, T result) : base(duration)
        {
            Result = result;
        }
    }
}
