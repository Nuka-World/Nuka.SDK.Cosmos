using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Nuka.SDK.Cosmos.Repositories.Document
{
    public interface IDocumentRepository<T> where T : class
    {
        Task<T> GetDocumentAsync(string partitionKey, string documentKey, ItemRequestOptions requestOptions = null);
        Task SetDocumentAsync(string partitionKey, T document, ItemRequestOptions requestOptions = null);
        Task DeleteDocumentAsync(string partitionKey, string documentKey, ItemRequestOptions requestOptions = null);
        Task<IList<T>> GetDocumentsAsync(string partitionKey, QueryRequestOptions requestOptions = null);
        Task<IList<T>> GetDocumentsByIdsAsync(string partitionKey, string[] itemIds, QueryRequestOptions requestOptions = null);
        Task DeleteDocumentsAsync(string partitionKey, QueryRequestOptions requestOptions = null);
        IQueryable<T> GetDocumentQuery(string partitionKey = null, int? maxItemCount = int.MaxValue);
    }
}