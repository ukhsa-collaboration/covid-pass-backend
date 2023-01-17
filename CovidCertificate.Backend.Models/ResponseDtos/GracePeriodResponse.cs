using System;

namespace CovidCertificate.Backend.Models.ResponseDtos
{
    public class GracePeriodResponse
    {
        public bool IsAllowed { get; private set; } // If the feature is disabled, the user is not allowed a grace period at current time, but might be later.
        public bool IsNew { get; private set; }
        public bool IsActive => CalculateTimeLeft() > new TimeSpan(0, 0, 0);
        public int CountdownTimeInHours { get; private set; }
        public string TimeLeft => GetTimeLeftFormatted();
        public DateTime StartedOn { get; private set; }
        public DateTime EndsOn => StartedOn.AddHours(CountdownTimeInHours);

        public GracePeriodResponse(bool isAllowed, bool isNew, DateTime startedOn, int countdownTimeInHours)
        {
            this.IsAllowed = isAllowed;
            this.IsNew = isNew;
            this.StartedOn = startedOn.ToUniversalTime();
            this.CountdownTimeInHours = countdownTimeInHours;
        }

        private string GetTimeLeftFormatted()
        {
            var timeLeft = CalculateTimeLeft();

            return (int)timeLeft.TotalHours + timeLeft.ToString(@"\:mm\:ss");
        }

        private TimeSpan CalculateTimeLeft()
        {
            if (EndsOn > DateTime.UtcNow)
            {
                return EndsOn - DateTime.UtcNow;
            }

            return new TimeSpan(0, 0, 0);
        }
    }
}
