using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Interfaces.TwoFactor
{
    public interface ISmsService
    {
        Task SendSmsAsync(Dictionary<string, dynamic> personalisation, string phoneNumber, string templateId);
    }
}
