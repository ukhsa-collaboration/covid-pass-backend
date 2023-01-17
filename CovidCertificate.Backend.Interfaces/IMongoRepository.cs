using CovidCertificate.Backend.Models.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IMongoRepository<TDocument> where TDocument : IMongoDocument
    {
        /// <summary>
        /// Finds the first TDocument satisfying the expression or null.
        /// </summary>
        /// <param name="filterExpression"></param>
        /// <returns>The first TDocument or null.</returns>
        TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression);

        /// <summary>
        /// Finds the first TDocument satisfying the expression or null.
        /// </summary>
        /// <param name="filterExpression"></param>
        /// <returns>A Task whose result is the first TDocument or null.</returns>

        Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression);
        /// <summary>
        /// Returns true if a TDocument satisfying the expression is found, otherwise false.
        /// </summary>
        /// <param name="filterExpression"></param>
        /// <returns>The boolean outcome.</returns>
        bool CheckIfExists(Expression<Func<TDocument, bool>> filterExpression);

        /// <summary>
        /// Returns true if a TDocument satisfying the expression is found, otherwise false.
        /// </summary>
        /// <param name="filterExpression"></param>
        /// <returns>A Task whose result is the boolean outcome.</returns>
        Task<bool> CheckIfExistsAsync(Expression<Func<TDocument, bool>> filterExpression);

        /// <summary>
        /// Finds all TDocument satisfying the expression.
        /// </summary>
        /// <param name="filterExpression"></param>
        /// <returns>An IEnumerable&lt;TDocument&gt; with all TDocument or an empty IEnumerable&lt;TDocument&gt;.</returns>
        IEnumerable<TDocument> FindAll(Expression<Func<TDocument, bool>> filterExpression);

        /// <summary>
        /// Finds all TDocument satisfying the expression.
        /// </summary>
        /// <param name="filterExpression"></param>
        /// <returns>A Task whose result is an IEnumerable&lt;TDocument&gt; with all TDocument or an empty IEnumerable&lt;TDocument&gt;.</returns>
        Task<IEnumerable<TDocument>> FindAllAsync(Expression<Func<TDocument, bool>> filterExpression); 
        
        /// <summary>
        /// Finds all TDocument satisfying the expression.
        /// </summary>
        /// <param name="filterExpression"></param>
        /// <returns>A long containing the number of documents returned by the filter expression. </returns>
        long Count(Expression<Func<TDocument, bool>> filterExpression);

        /// <summary>
        /// Finds all TDocument satisfying the expression.
        /// </summary>
        /// <param name="filterExpression"></param>
        /// <returns>A long containing the number of documents returned by the filter expression</returns>
        Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression);


        /// <summary>
        /// Finds the first TDocument satisfying the id or null.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The first TDocument or null.</returns>
        TDocument FindById(string id);

        /// <summary>
        /// Finds the first TDocument satisfying the id or null.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A Task whose result is the first TDocument or null.</returns>
        Task<TDocument> FindByIdAsync(string id);

        /// <summary>
        /// Inserts a single TDocument.
        /// </summary>
        /// <param name="document"></param>
        void InsertOne(TDocument document);

        /// <summary>
        /// Inserts a single TDocument.
        /// </summary>
        /// <param name="document"></param>
        /// <returns>A Task whose result is the result of the insert operation.</returns>
        Task InsertOneAsync(TDocument document);

        /// <summary>
        /// Inserts multiple TDocument.
        /// </summary>
        /// <param name="documents"></param>
        void InsertMany(ICollection<TDocument> documents);

        /// <summary>
        /// Inserts multiple TDocument.
        /// </summary>
        /// <param name="documents"></param>
        /// <returns>A Task whose result is the result of the insert operation.</returns>
        Task InsertManyAsync(ICollection<TDocument> documents);

        /// <summary>
        /// Update one TDocument
        /// </summary>
        /// <param name="document"></param>
        /// <param name="filterExpression"></param>
        /// <returns>A Task whose result is the result of the update operation</returns>
        Task UpdateOneAsync(UpdateDefinition<TDocument> document, Expression<Func<TDocument, bool>> filterExpression, bool isUpsert = false);

        /// <summary>
        /// Finds the first TDocument with a matching document id and replaces it atomically. 
        /// </summary>
        /// <param name="document"></param>
        void ReplaceOne(TDocument document, bool isUpsert = false);

        /// <summary>
        /// Finds the first TDocument with a matching document id and replaces it atomically. 
        /// </summary>
        /// <param name="document"></param>
        /// <returns>A Task whose result is the result of the update operation.</returns>
        Task ReplaceOneAsync(TDocument document, bool isUpsert = false);

        /// <summary>
        /// Finds the first TDocument satisfying the expression and replaces it atomically. 
        /// </summary>
        /// <param name="document"></param>
        /// <returns>A Task whose result is the result of the update operation.</returns>
        Task ReplaceOneAsync(TDocument document, Expression<Func<TDocument, bool>> filterExpression, bool isUpsert = false);

        /// <summary>
        /// Finds the first TDocument satisfying the expression and deletes it atomically.
        /// </summary>
        /// <param name="filterExpression"></param>
        void DeleteOne(Expression<Func<TDocument, bool>> filterExpression);

        /// <summary>
        /// Finds the first TDocument satisfying the expression and deletes it atomically.
        /// </summary>
        /// <param name="filterExpression"></param>
        /// <returns>A Task whose result is the result of the delete operation.</returns>
        Task<TDocument> DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression);

        /// <summary>
        /// Finds the first TDocument satisfying the id and deletes it atomically.
        /// </summary>
        /// <param name="id"></param>
        void DeleteById(string id);

        /// <summary>
        /// Finds the first TDocument satisfying the id and deletes it atomically.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A Task whose result is the result of the delete operation.</returns>
        Task DeleteByIdAsync(string id);

        /// <summary>
        /// Finds all TDocument satisfying the expression and deletes them.
        /// </summary>
        /// <param name="filterExpression"></param>
        void DeleteMany(Expression<Func<TDocument, bool>> filterExpression);

        /// <summary>
        /// Finds all TDocument satisfying the expression and deletes them.
        /// </summary>
        /// <param name="filterExpression"></param>
        /// <returns>A Task whose result is the result of the delete operation.</returns>
        Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression);
    }
}
