
using System.Collections.Generic;

namespace CovidCertificate.Backend.Interfaces
{
    public interface INhsKeyRing
    {
        string SignData(Dictionary<string, object> payload);
    }
}
