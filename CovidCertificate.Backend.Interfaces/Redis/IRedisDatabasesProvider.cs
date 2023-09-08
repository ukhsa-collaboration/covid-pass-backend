using System.Collections.Generic;
using StackExchange.Redis;

namespace CovidCertificate.Backend.Interfaces.Redis
{
    public interface IRedisDatabasesProvider
    {
        List<IDatabase> GetDatabases();
    }
}
