namespace CovidCertificate.Backend.Models.ResponseDtos
{
    public class InternationalQrResponse
    {
        public InternationalQrResponse(QRcodeResponse vaccinationQrResponse, QRcodeResponse recoveryQrResponse)
        {
            VaccinationQrResponse = vaccinationQrResponse;
            RecoveryQrResponse = recoveryQrResponse;
        }
        public QRcodeResponse VaccinationQrResponse;

        public QRcodeResponse RecoveryQrResponse;
    }
}
