using System.Collections.Generic;

namespace CovidCertificate.Backend.Models.Interfaces
{
    public interface IEmailModel
    {
        Dictionary<string, dynamic> GetPersonalisation();

        string EmailAddress { get; }
    }
}
