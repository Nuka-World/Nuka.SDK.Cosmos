using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nuka.SDK.Cosmos.Models;
using Nuka.SDK.Cosmos.Options;

namespace Nuka.SDK.Cosmos.Repositories.Document
{
    internal class CosmosDocumentRepository<T> : IDocumentRepository<T> where T : class, IDocument
    {
        private readonly string _collectionName;
        private readonly ILogger<CosmosDocumentRepository<T>> _logger;
        private readonly Container _container;
        private readonly bool _enableSoftDelete;
        private readonly int _softDeleteExpiry;
        private readonly ItemRequestOptions _requestOptions;
        private readonly QueryRequestOptions _queryRequestOptions;
        private readonly JsonSerializer _serializer = new JsonSerializer();

        public CosmosDocumentRepository(
            CosmosClient client,
            IOptions<CosmosOptions> options,
            string collectionName,
            string partitionKeyName,
            ILogger<CosmosDocumentRepository<T>> logger)
        {
            _logger = logger;
            _collectionName = collectionName;
            _container = client.GetContainer(options.Value.DatabaseName, collectionName);
            _enableSoftDelete = options.Value.EnableSoftDelete;
            _softDeleteExpiry = options.Value.SoftDeleteExpiry;

            ConsistencyLevel? level = ConsistencyLevelMapping.GetConsistencyLevel(options.Value.ConsistencyLevel);
            if (level != null)
            {
                _requestOptions = new ItemRequestOptions {ConsistencyLevel = level};
                _queryRequestOptions = new QueryRequestOptions {ConsistencyLevel = level};
            }

            var partitionKeyProp = typeof(T)
                .GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<JsonPropertyAttribute>().PropertyName == partitionKeyName);

            if (partitionKeyProp == null)
                partitionKeyProp =
                    typeof(T).GetProperty(partitionKeyName, BindingFlags.Instance | BindingFlags.Public);

            if (partitionKeyProp == null)
                throw new ArgumentException(
                    $"Document type {typeof(T).Name} does not contain a Property (or JsonProperty) named {partitionKeyName}");

            if (!partitionKeyProp.CanWrite)
                throw new ArgumentException(
                    $"Property (or JsonProperty) named {partitionKeyName} for Document type {typeof(T).Name} is not writeable");
        }

        public Task<T> GetDocumentAsync(
            string partitionKey,
            string documentKey,
            ItemRequestOptions requestOptions = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
                throw new ArgumentNullException(nameof(partitionKey));

            if (string.IsNullOrEmpty(documentKey))
                throw new ArgumentNullException(nameof(documentKey));

            return GetDocumentInternalAsync(partitionKey, documentKey, requestOptions ?? _requestOptions);
        }

        private async Task<T> GetDocumentInternalAsync(
            string partitionKey,
            string documentKey,
            ItemRequestOptions requestOptions = null)
        {
            var logProperties = new Dictionary<string, string>
            {
                ["collection_name"] = _collectionName,
                ["partition_key"] = partitionKey,
                ["document_key"] = documentKey,
                ["query"] = "get item by id",
                ["consistency_level"] = requestOptions?.ConsistencyLevel?.ToString()
            };

            try
            {
                var response =
                    await _container.ReadItemAsync<T>(documentKey, new PartitionKey(partitionKey), _requestOptions);

                if (_enableSoftDelete == false)
                    return response.Resource;

                var ttlitem = response.Resource as IExpiringDocument;
                return ttlitem?._deleted == true ? null : response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "cosmos-error-get-document {@context}", logProperties);
                throw;
            }
        }

        public Task SetDocumentAsync(
            string partitionKey,
            T document,
            ItemRequestOptions requestOptions = null)
        {
            if (string.IsNullOrEmpty(partitionKey)) throw new ArgumentNullException(nameof(partitionKey));
            if (document == null) throw new ArgumentNullException(nameof(document));

            return SetDocumentInternalAsync(partitionKey, document, requestOptions ?? _requestOptions);
        }

        private async Task SetDocumentInternalAsync(
            string partitionKey,
            T document,
            ItemRequestOptions requestOptions = null)
        {
            var logProperties = new Dictionary<string, string>
            {
                ["collection_name"] = _collectionName,
                ["partition_key"] = partitionKey,
                ["document_key"] = document.Id,
                ["consistency_level"] = requestOptions?.ConsistencyLevel?.ToString()
            };

            try
            {
                await _container.UpsertItemAsync(
                    document,
                    new PartitionKey(partitionKey),
                    requestOptions ?? _requestOptions);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "cosmos-error-set-document {@context}", logProperties);
                throw;
            }
        }

        public Task DeleteDocumentAsync(
            string partitionKey,
            string documentKey,
            ItemRequestOptions requestOptions = null)
        {
            if (string.IsNullOrEmpty(partitionKey)) throw new ArgumentNullException(nameof(partitionKey));
            if (string.IsNullOrEmpty(documentKey)) throw new ArgumentNullException(nameof(documentKey));
            var isExpiringType = typeof(T).GetInterfaces().Contains(typeof(IExpiringDocument));

            if (_enableSoftDelete && isExpiringType)
                return SoftDeleteDocumentInternalAsync(partitionKey, documentKey, requestOptions ?? _requestOptions);
            else
            {
                return DeleteDocumentInternalAsync(partitionKey, documentKey, requestOptions ?? _requestOptions);
            }
        }

        private async Task DeleteDocumentInternalAsync(
            string partitionKey,
            string documentKey,
            ItemRequestOptions requestOptions)
        {
            var logProperties = new Dictionary<string, string>
            {
                ["collection_name"] = _collectionName,
                ["partition_key"] = partitionKey,
                ["document_key"] = documentKey,
                ["consistency_level"] = requestOptions?.ConsistencyLevel?.ToString()
            };

            try
            {
                await _container.DeleteItemAsync<T>(documentKey, new PartitionKey(partitionKey));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug(ex, "cosmos-document-not-found {@context}", logProperties);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "cosmos-error-delete-document {@context}", logProperties);
                throw;
            }
        }

        private async Task SoftDeleteDocumentInternalAsync(string partitionKey, string documentKey,
            ItemRequestOptions requestOptions)
        {
            var _ = new Dictionary<string, string>
            {
                ["collection_name"] = _collectionName,
                ["partition_key"] = partitionKey,
                ["document_key"] = documentKey,
                ["consistency_level"] = requestOptions?.ConsistencyLevel?.ToString()
            };

            var item = await GetDocumentInternalAsync(partitionKey, documentKey, requestOptions);
            if (item == null)
            {
                return;
            }

            var ttlitem = item as IExpiringDocument;
            ttlitem.ttl = _softDeleteExpiry;
            ttlitem._deleted = true;
            var recast = ttlitem as T;
            await SetDocumentInternalAsync(partitionKey, recast, requestOptions);
        }

        public Task<IList<T>> GetDocumentsAsync(string partitionKey, QueryRequestOptions requestOptions = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
                throw new ArgumentNullException(nameof(partitionKey));
            return GetDocumentsInternalAsync(partitionKey, requestOptions ?? _queryRequestOptions);
        }

        private async Task<IList<T>> GetDocumentsInternalAsync(
            string partitionKey,
            QueryRequestOptions queryRequestOptions)
        {
            var logProperties = new Dictionary<string, string>
            {
                ["collection_name"] = _collectionName,
                ["grouping"] = partitionKey,
                ["consistency_level"] = queryRequestOptions?.ConsistencyLevel?.ToString()
            };

            try
            {
                queryRequestOptions ??= new QueryRequestOptions();
                queryRequestOptions.PartitionKey = new PartitionKey(partitionKey);

                var iterator = _container.GetItemQueryStreamIterator(requestOptions: queryRequestOptions);
                var documents = await ProcessQueryStreamAsync(iterator, logProperties);
                return !_enableSoftDelete
                    ? documents
                    : documents.Where(d => ((IExpiringDocument) d)._deleted != true).ToList();
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "cosmos-error-get-filtered-documents {@context}", logProperties);
                throw;
            }
        }
        
        public Task<IList<T>> GetDocumentsByIdsAsync(
            string partitionKey,
            string[] itemIds,
            QueryRequestOptions requestOptions = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
                throw new ArgumentNullException(nameof(partitionKey));

            var filter = CreateSqlFilterFromArray(itemIds);
            return GetDocumentsByIdsInternalAsync(partitionKey, filter, requestOptions ?? _queryRequestOptions);
        }

        private async Task<IList<T>> GetDocumentsByIdsInternalAsync(
            string partitionKey, 
            QueryDefinition filter, 
            QueryRequestOptions queryRequestOptions = null)
        {
            var logProperties = new Dictionary<string, string>
            {
                ["collection_name"] = _collectionName,
                ["grouping"] = partitionKey,
                ["consistency_level"] = queryRequestOptions?.ConsistencyLevel?.ToString()
            };
            
            try
            {
                queryRequestOptions ??= new QueryRequestOptions();
                queryRequestOptions.PartitionKey = new PartitionKey(partitionKey);

                var streamResultSet = _container.GetItemQueryStreamIterator(requestOptions: queryRequestOptions, queryDefinition: filter);

                var documents =  await ProcessQueryStreamAsync(streamResultSet, logProperties);
                return _enableSoftDelete == false ? documents : documents.Where(d => (d as IExpiringDocument)?._deleted != true).ToList();
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "cosmos-error-get-filtered-documents {@context}", logProperties);
                throw;
            }
        }

        public Task DeleteDocumentsAsync(string partitionKey, QueryRequestOptions requestOptions = null)
        {
            if (string.IsNullOrEmpty(partitionKey))
                throw new ArgumentNullException(nameof(partitionKey));
            
            return DeleteDocumentsInternalAsync(partitionKey, requestOptions ?? _queryRequestOptions);
        }

        private async Task DeleteDocumentsInternalAsync(string partitionKey, QueryRequestOptions queryRequestOptions)
        {
            var logProperties = new Dictionary<string, string>
            {
                ["collection_name"] = _collectionName,
                ["partition_key"] = partitionKey,
                ["consistency_level"] = queryRequestOptions?.ConsistencyLevel?.ToString()
            };

            try
            {
                var documents = await GetDocumentsAsync(partitionKey, queryRequestOptions);
                if (documents != null)
                {
                    foreach (var document in documents)
                    {
                        await DeleteDocumentAsync(partitionKey, document.Id,
                            new ItemRequestOptions {ConsistencyLevel = queryRequestOptions.ConsistencyLevel});
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "cosmos-error-delete-documents {@context}", logProperties);
                throw;
            }
        }

        public IQueryable<T> GetDocumentQuery(string partitionKey = null, int? maxItemCount = int.MaxValue)
        {
            var queryRequestOptions = new QueryRequestOptions { MaxItemCount = maxItemCount};
            if (string.IsNullOrWhiteSpace(partitionKey))
                queryRequestOptions.EnableScanInQuery = true;
            else
                queryRequestOptions.PartitionKey = new PartitionKey(partitionKey);

            var isExpiringType = typeof(T).GetInterfaces().Contains(typeof(IExpiringDocument));
            if (_enableSoftDelete && isExpiringType)
            {
                return _container
                    .GetItemLinqQueryable<T>(requestOptions: queryRequestOptions, allowSynchronousQueryExecution: true)
                    .Where(i => (i is IExpiringDocument) && ((IExpiringDocument) i)._deleted != true);
            }
            else
            {
                return _container
                    .GetItemLinqQueryable<T>(requestOptions: queryRequestOptions, allowSynchronousQueryExecution: true);
            }
        }

        private async Task<List<T>> ProcessQueryStreamAsync(
            FeedIterator iterator,
            IDictionary<string, string> logProperties)
        {
            var items = new List<T>();
            while (iterator.HasMoreResults)
            {
                using var responseMessage = await iterator.ReadNextAsync();
                if (responseMessage.IsSuccessStatusCode)
                {
                    var streamResponse = FromStream<dynamic>(responseMessage.Content);
                    var responseItems = streamResponse.Documents.ToObject<List<T>>();
                    items.AddRange(responseItems);
                }
                else
                {
                    logProperties["response_code"] = responseMessage.StatusCode.ToString();
                    logProperties["response_error"] = responseMessage.ErrorMessage;
                    _logger.LogDebug("cosmos-error-get-documents-query-stream-failed {@context}", logProperties);
                    throw new Exception(
                        $"Error response received in stream response from Cosmos. Http Status Code: {responseMessage.StatusCode.ToString()}. Error Message: ${responseMessage.ErrorMessage}");
                }
            }

            return items;
        }
        
        private QueryDefinition CreateSqlFilterFromArray(IReadOnlyList<string> itemIds)
        {
            if (itemIds == null || itemIds.Count == 0)
                return new QueryDefinition("SELECT * FROM c");

            if (itemIds.Count == 1)
            {
                return
                    new QueryDefinition("SELECT * FROM c WHERE c.id = @item_0")
                        .WithParameter("@item_0", itemIds[0]);
            }

            // List contains multiple items
            var queryParameters = new Dictionary<string, string>();
            const string generalQuery = @"SELECT * FROM c WHERE c.id IN ({0})";
            for (var i = 0; i < itemIds.Count; i++)
            {
                var name = "@item_" + i;
                queryParameters[name] = itemIds[i];
            }

            var nameList = string.Join(",", queryParameters.Keys);
            var queryText = string.Format(generalQuery, nameList);
            var spec = new QueryDefinition(queryText);
            return queryParameters.Aggregate(spec, (current, queryParameter) => current.WithParameter(queryParameter.Key, queryParameter.Value));
        }

        private T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                using (var sr = new StreamReader(stream))
                {
                    using (var jsonTextReader = new JsonTextReader(sr))
                    {
                        jsonTextReader.DateParseHandling = DateParseHandling.None;
                        return _serializer.Deserialize<T>(jsonTextReader);
                    }
                }
            }
        }
    }
}