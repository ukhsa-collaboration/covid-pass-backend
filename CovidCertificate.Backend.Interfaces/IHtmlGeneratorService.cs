using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.RequestDtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IHtmlGeneratorService
    {
        Task<string> GenerateHtmlAsync(GetHtmlRequestDto dto, string templateFolder);
        Task<string> GetPassHtmlAsync(dynamic model, PassData type, string langCode = "en");
        Task<string> GetPassTermsAsync(string langCode = "en");
        Task<Dictionary<string, string>> GetPassLabelsAsync(string langCode = "en");
    }
}
