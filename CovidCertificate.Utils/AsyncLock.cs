using System;
using System.Threading;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Utils
{
    public class AsyncLock : IDisposable
    {
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public async Task<AsyncLock> LockAsync()
        {
            await semaphore.WaitAsync();
            return this;
        }

        public void Dispose()
        {
            semaphore.Release();
        }
    }
}
