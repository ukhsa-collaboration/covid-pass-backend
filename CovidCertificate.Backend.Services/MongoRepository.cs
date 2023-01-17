using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.Settings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CovidCertificate.Backend.Utils.Extensions;
using CovidCertificate.Backend.Utils.Timing;
using Polly;
using Polly.Retry;

namespace CovidCertificate.Backend.Services
{
    public class MongoRepository<TDocument> : IMongoRepository<TDocument> where TDocument : IMongoDocument
    {
        private readonly ILogger<MongoRepository<TDocument>> logger;
        private readonly IMongoCollection<TDocument> collection;
        private AsyncRetryPolicy asyncRetryPolicy;
        private RetryPolicy retryPolicy;
        private readonly uint maxDeleteExecutionTimeWarning = uint.MaxValue;
        private readonly uint maxReadExecutionTimeWarning = uint.MaxValue;
        private readonly uint maxWriteExecutionTimeWarning = uint.MaxValue;
        private readonly uint maxUpdateExecutionTimeWarning = uint.MaxValue;
        private readonly int retryCount = 0;
        private readonly int retrySleepDuration = 0;

        public MongoRepository(IMongoClient mongoClient, MongoDbSettings settings, ILogger<MongoRepository<TDocument>> logger)
        {
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            this.collection = database.GetCollection<TDocument>(GetCollectionName(typeof(TDocument)));
            this.logger = logger;
            this.maxDeleteExecutionTimeWarning = settings.MaxDeleteExecutionTimeWarning;
            this.maxReadExecutionTimeWarning = settings.MaxReadExecutionTimeWarning;
            this.maxWriteExecutionTimeWarning = settings.MaxWriteExecutionTimeWarning;
            this.maxUpdateExecutionTimeWarning = settings.MaxUpdateExecutionTimeWarning;
            this.retryCount = settings.RetryCount;
            this.retrySleepDuration = settings.RetrySleepDuration;

            InstantiateRetryPolicies();
        }

        #region Core

        public TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartFunction(() => collection.Find(filterExpression).FirstOrDefault()));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxReadExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum read execution time exceeded for {nameof(FindOne)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            return timeMeasurerResult.Result;
        }

        public async Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            logger.LogInformation(LogType.CosmosDb, "FindOneAsync was invoked");

            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartFunctionAsync(() => collection.Find(filterExpression).FirstOrDefaultAsync()));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxReadExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum read execution time exceeded for {nameof(FindOneAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogTraceAndDebug(LogType.CosmosDb, "FindOneAsync has finished");

            return timeMeasurerResult.Result;
        }

        public bool CheckIfExists(Expression<Func<TDocument, bool>> filterExpression)
        {
            logger.LogInformation(LogType.CosmosDb, "CheckIfExists was invoked");

            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartFunction(() => collection.Find(filterExpression).FirstOrDefault()));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxReadExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum read execution time exceeded for {nameof(CheckIfExists)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogTraceAndDebug(LogType.CosmosDb, "CheckIfExists has finished");

            return timeMeasurerResult.Result != null;
        }

        public async Task<bool> CheckIfExistsAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            logger.LogInformation(LogType.CosmosDb, "CheckIfExists was invoked");

            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartFunctionAsync(() => collection.Find(filterExpression).FirstOrDefaultAsync()));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxReadExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum read execution time exceeded for {nameof(CheckIfExistsAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogTraceAndDebug(LogType.CosmosDb, "CheckIfExists has finished");

            return timeMeasurerResult.Result != null;
        }

        public IEnumerable<TDocument> FindAll(Expression<Func<TDocument, bool>> filterExpression)
        {
            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartFunction(() => collection.Find(filterExpression).ToEnumerable()));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxReadExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum read execution time exceeded for {nameof(FindAll)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            return timeMeasurerResult.Result;
        }

        public async Task<IEnumerable<TDocument>> FindAllAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            logger.LogInformation(LogType.CosmosDb, "FindAllAsync was invoked");

            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartFunctionAsync(async () => await collection.Find(filterExpression).ToListAsync()));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxReadExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum read execution time exceeded for {nameof(FindAllAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogTraceAndDebug(LogType.CosmosDb, "FindAllAsync has finished");

            return timeMeasurerResult.Result;
        }

        public long Count(Expression<Func<TDocument, bool>> filterExpression)
        {
            logger.LogInformation(LogType.CosmosDb, "Count was invoked");

            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartFunction(() => collection.CountDocuments(filterExpression)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxReadExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum read execution time exceeded for {nameof(FindAllAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogInformation(LogType.CosmosDb, "Count has finished");

            return timeMeasurerResult.Result;
        }

        public async Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            logger.LogInformation(LogType.CosmosDb, "CountAsync was invoked");

            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartFunctionAsync(async () => await collection.CountDocumentsAsync(filterExpression)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxReadExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum read execution time exceeded for {nameof(CountAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogInformation(LogType.CosmosDb, "CountAsync has finished");

            return timeMeasurerResult.Result;
        }


        public TDocument FindById(string id)
        {
            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartFunction(() =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
                return collection.Find(filter).SingleOrDefault();
            }));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxReadExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum read execution time exceeded for {nameof(FindById)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            return timeMeasurerResult.Result;
        }

        public async Task<TDocument> FindByIdAsync(string id)
        {
            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartFunctionAsync(async () =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
                return await collection.Find(filter).SingleOrDefaultAsync();
            }));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxReadExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum read execution time exceeded for {nameof(FindByIdAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            return timeMeasurerResult.Result;
        }

        public void InsertOne(TDocument document)
        {
            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartAction(() => collection.InsertOne(document)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxWriteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum write execution time exceeded for {nameof(InsertOne)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");
        }

        public async Task InsertOneAsync(TDocument document)
        {
            logger.LogInformation(LogType.CosmosDb, "InsertOneAsync was invoked");

            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartActionAsync(() => collection.InsertOneAsync(document)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxWriteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum write execution time exceeded for {nameof(InsertOneAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogTraceAndDebug(LogType.CosmosDb, "InsertOneAsync has finished");
        }

        public void InsertMany(ICollection<TDocument> documents)
        {
            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartAction(() => collection.InsertMany(documents)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxWriteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum write execution time exceeded for {nameof(InsertMany)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");
        }

        public async Task InsertManyAsync(ICollection<TDocument> documents)
        {
            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartActionAsync(() => collection.InsertManyAsync(documents)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxWriteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum write execution time exceeded for {nameof(InsertManyAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");
        }

        public async Task UpdateOneAsync(UpdateDefinition<TDocument> document, Expression<Func<TDocument, bool>> filterExpression, bool isUpsert = false)
        {
            logger.LogInformation(LogType.CosmosDb, $"{nameof(UpdateOneAsync)} was invoked");
            UpdateOptions updateOptions = new UpdateOptions { IsUpsert = isUpsert };
            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartActionAsync(() => collection.UpdateOneAsync(filterExpression, document, updateOptions)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxWriteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum write execution time exceeded for {nameof(UpdateOneAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");
        }

        public void ReplaceOne(TDocument document, bool isUpsert = false)
        {
            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartAction(() =>
            {
                var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);

                var options = new FindOneAndReplaceOptions<TDocument, TDocument>() { IsUpsert = isUpsert };

                collection.FindOneAndReplace(filter, document, options);
            }));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxUpdateExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum update execution time exceeded for {nameof(ReplaceOne)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");
        }

        public async Task ReplaceOneAsync(TDocument document, bool isUpsert = false)
        {
            logger.LogInformation(LogType.CosmosDb, "ReplaceOneAsync was invoked");

            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartActionAsync(async () =>
            {
                var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);

                var options = new FindOneAndReplaceOptions<TDocument, TDocument>() { IsUpsert = isUpsert };

                await collection.FindOneAndReplaceAsync(filter, document, options);
            }));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxUpdateExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum update execution time exceeded for {nameof(ReplaceOneAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogTraceAndDebug(LogType.CosmosDb, "ReplaceOneAsync has finished");
        }

        public async Task ReplaceOneAsync(TDocument document, Expression<Func<TDocument, bool>> filterExpression, bool isUpsert = false)
        {
            logger.LogInformation(LogType.CosmosDb, "ReplaceOneAsync was invoked");

            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartActionAsync(async () =>
            {
                await collection.FindOneAndReplaceAsync(filterExpression, document);
            }));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxUpdateExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum update execution time exceeded for {nameof(ReplaceOneAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogTraceAndDebug(LogType.CosmosDb, "ReplaceOneAsync has finished");
        }

        public void DeleteOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartAction(() => collection.FindOneAndDelete(filterExpression)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxDeleteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum delete execution time exceeded for {nameof(DeleteOne)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");
        }

        public async Task<TDocument> DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            logger.LogInformation(LogType.CosmosDb, "DeleteOneAsync was invoked");
            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartFunctionAsync(() => collection.FindOneAndDeleteAsync(filterExpression)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxDeleteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum delete execution time exceeded for {nameof(DeleteOneAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogTraceAndDebug(LogType.CosmosDb, "DeleteOneAsync has finished");
            return timeMeasurerResult.Result;
        }

        public void DeleteById(string id)
        {
            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartAction(() =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
                collection.FindOneAndDelete(filter);
            }));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxDeleteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum delete execution time exceeded for {nameof(DeleteById)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");
        }

        public async Task DeleteByIdAsync(string id)
        {
            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartActionAsync(async () =>
            {
                var objectId = new ObjectId(id);
                var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
                await collection.FindOneAndDeleteAsync(filter);
            }));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxDeleteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum delete execution time exceeded for {nameof(DeleteByIdAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");
        }

        public void DeleteMany(Expression<Func<TDocument, bool>> filterExpression)
        {
            var timeMeasurerResult = RetryOnFailure(() => TimeMeasurer.StartAction(() => collection.DeleteMany(filterExpression)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxDeleteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum delete execution time exceeded for {nameof(DeleteMany)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");
        }

        public async Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            logger.LogInformation(LogType.CosmosDb, "DeleteManyAsync was invoked");

            var timeMeasurerResult = await RetryOnFailureAsync(() => TimeMeasurer.StartActionAsync(() => collection.DeleteManyAsync(filterExpression)));

            if (timeMeasurerResult.Duration.TotalMilliseconds >= maxDeleteExecutionTimeWarning)
                logger.LogWarning(LogType.CosmosDb, $"Maximum delete execution time exceeded for {nameof(DeleteManyAsync)} with an execution time of {timeMeasurerResult.Duration.TotalMilliseconds} ms");

            logger.LogTraceAndDebug(LogType.CosmosDb, "DeleteManyAsync has finished");
        }
        #endregion

        #region Helper Methods

        private protected string GetCollectionName(Type documentType)
        {
            return ((Collection)documentType.GetCustomAttributes(typeof(Collection), true).FirstOrDefault())?.CollectionName;
        }

        private protected void InstantiateRetryPolicies()
        {
            retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    retryCount,
                    _ => TimeSpan.FromMilliseconds(retrySleepDuration),
                    (response, _, retries, context) =>
                        logger.LogWarning($"Error in MongoRepository - on attempt no. {retries} out of {retryCount}.{(retries != retryCount ? $" Retrying in {retrySleepDuration}ms." : "")} Error message: {response.Message}"));

            asyncRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount,
                    _ => TimeSpan.FromMilliseconds(retrySleepDuration),
                    (response, _, retries, context) =>
                        logger.LogWarning($"Error in MongoRepository - on attempt no. {retries} out of {retryCount}.{(retries != retryCount ? $" Retrying in {retrySleepDuration}ms." : "")} Error message: {response.Message}"));
        }

        private T RetryOnFailure<T>(Func<T> funcToRun) 
            => retryPolicy.Execute(() => funcToRun());
        
        private Task<T> RetryOnFailureAsync<T>(Func<Task<T>> funcToRun) 
            => asyncRetryPolicy.ExecuteAsync(async () => await funcToRun());
        #endregion
    }
}
