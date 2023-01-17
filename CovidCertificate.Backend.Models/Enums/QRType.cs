namespace CovidCertificate.Backend.Models.Enums
{
    public enum QRType
    {
        Domestic,
        International,
        Recovery
    }

    public enum PassData
    {
        recovery,
        vaccine
    }

    public enum PDFType
    {
        VaccineAndRecovery,
        Vaccine,
        Recovery
    }
}
