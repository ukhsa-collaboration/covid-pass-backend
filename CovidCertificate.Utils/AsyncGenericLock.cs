using System.Threading;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Utils
{
    public static class AsyncGenericLock<T>
    {
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public static async Task LockAsync()
        {
            await semaphore.WaitAsync();
        }

        public static void Release()
        {
            semaphore.Release();
        }
    }
}
