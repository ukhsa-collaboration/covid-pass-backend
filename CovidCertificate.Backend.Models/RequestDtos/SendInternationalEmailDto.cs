using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Validators;
using FluentValidation;

namespace CovidCertificate.Backend.Models.RequestDtos
{
    public class SendInternationalEmailDto
    {
        public string Email { get; set; }
        public async Task ValidateObjectAndThrowOnFailuresAsync() => await new SendInternationalEmailDtoValidator().ValidateAndThrowAsync(this);
    }
}
