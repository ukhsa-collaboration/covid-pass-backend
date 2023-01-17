using System.Collections.Generic;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IBoosterValidityService
    {
        bool IsBoosterWithinCorrectTimeFrame(IEnumerable<IGenericResult> results);
        IEnumerable<IGenericResult> RemoveBoosters(IEnumerable<IGenericResult> results);
    }
}
