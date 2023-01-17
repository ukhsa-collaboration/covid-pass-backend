
namespace CovidCertificate.Backend.Models
{
    // Please refer to https://tools.ietf.org/html/rfc8152#page-39
    public enum Algorithms
    {
        SHA256withECDSA = -7,
        SHA384withECDSA = -35,
        SHA512withECDSA = -36,
        SHA256withRSAPSS = -37,
        SHA384withRSAPSS = -38,
        SHA512withRSAPSS = -39
    }
}
