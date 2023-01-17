using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    /// <summary>
    /// Queue service adds a series of messages or objects to a queue, whether this is cloud or local hosted
    /// </summary>
    public interface IQueueService
    {
        /// <summary>
        /// Adds an object to the message queue
        /// </summary>
        /// <typeparam name="T">The object type to add</typeparam>
        /// <param name="queueName">The queue to add it too</param>
        /// <param name="messageObject">The object to add</param>
        /// <returns></returns>
        Task<bool> SendMessageAsync<T>(string queueName, T messageObject) where T : class;
    }
}
