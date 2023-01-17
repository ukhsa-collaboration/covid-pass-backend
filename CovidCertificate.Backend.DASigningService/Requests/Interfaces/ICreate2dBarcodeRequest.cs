using FluentValidation.Results;

namespace CovidCertificate.Backend.DASigningService.Requests.Interfaces
{
    public interface ICreate2DBarcodeRequest
    {
        ValidationResult Validate();
        string GetRegionSubscriptionNameHeader();

        string ValidFrom { get; }
        string ValidTo { get; }
    }
}
