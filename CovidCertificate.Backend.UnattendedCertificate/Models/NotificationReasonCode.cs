using System;
using System.Collections.Generic;
using System.Text;

namespace CovidCertificate.Backend.UnattendedCertificate.Models
{
    public enum NotificationReasonCode
    {
        /// <summary>
        /// No immunization or recovery certificate available.
        /// </summary>
        MissingRecord = 3006,

        /// <summary>
        /// Vaccination record doesn't show the correct dosage number, timing, or manufacturer requirements.
        /// </summary>
        IncompleteVaccinationRecord = 3012,
    }
}
