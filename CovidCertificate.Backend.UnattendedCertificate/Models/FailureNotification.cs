using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.UnattendedCertificate.Models
{
    public class FailureNotification
    {
        public NotificationReasonCode ReasonCode;
        public LetterRequest Request;
    }
}
