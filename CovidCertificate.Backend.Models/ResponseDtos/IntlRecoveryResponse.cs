using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Models.ResponseDtos
{
    public class IntlRecoveryResponse : TestResultNhs
    {
        public string QRCode;
        public IntlRecoveryResponse(TestResultNhs t, string qr) : base(t)
        {
            QRCode = qr;
        }
    }
}
