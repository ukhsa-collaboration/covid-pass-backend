using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Models.ResponseDtos
{
    public class IntlVaccineResponse: Vaccine
    {
        public string QRCode;

        public IntlVaccineResponse(Vaccine v, string qr):base(v)
        {

            QRCode = qr;

        }
    }
}
