using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Interfaces.TwoFactor
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email from a template using the input model, we get the extra properties we need for the email from the 
        /// input interface
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="emailContent"></param>
        /// <param name="templateId"></param>
        /// <returns></returns>
        public Task SendEmailAsync<T>(T emailContent, string templateId) where T : IEmailModel;

        public Task SendPdfSizeFailureEmailAsync(string emailAddress, string govUkNotifyTemplateGuid);
    }
}
