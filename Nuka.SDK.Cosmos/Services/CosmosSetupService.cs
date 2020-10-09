using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Nuka.SDK.Cosmos.Options;

namespace Nuka.SDK.Cosmos.Services
{
    [ExcludeFromCodeCoverage]
    public class CosmosSetupService : ICosmosSetupService
    {
        private const int DefaultFixedOfferedThroughput = 400;
        private const int DefaultAutoScaleMaxThroughput = 4000;

        private readonly CosmosClient _cosmosClient;
        private readonly CosmosOptions _cosmosOptions;
        private readonly ILogger<CosmosSetupService> _logger;

        private Database _db;

        public CosmosSetupService(
            CosmosClient cosmosClient,
            CosmosOptions cosmosOptions,
            ILogger<CosmosSetupService> logger)
        {
            _cosmosClient = cosmosClient;
            _cosmosOptions = cosmosOptions;
            _logger = logger;
        }

        public async Task InitializeCosmosDbAsync(CancellationToken cancellationToken)
        {
            await InitializeCosmosDbAsyncInternal(cancellationToken);
        }

        private async Task InitializeCosmosDbAsyncInternal(CancellationToken cancellationToken)
        {
            _db = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_cosmosOptions.DatabaseName,
                cancellationToken: cancellationToken);

            var collectionSetupTasks = _cosmosOptions
                .Documents
                .Select(document => InitializeCollectionAsync(document, cancellationToken));

            await Task.WhenAll(collectionSetupTasks);
        }

        private async Task InitializeCollectionAsync(
            DocumentOptions documentOptions,
            CancellationToken cancellationToken)
        {
            var logProperties = new Dictionary<string, string>
            {
                ["db_name"] = _cosmosOptions.DatabaseName,
                ["collection_name"] = documentOptions.Name
            };

            try
            {
                var containerProperties =
                    new ContainerProperties(documentOptions.Name, $"/{documentOptions.PartitionKeyName}")
                    {
                        DefaultTimeToLive =
                            documentOptions.TimeToLiveDays == -1
                                ? -1
                                : Convert.ToInt32(TimeSpan.FromDays(documentOptions.TimeToLiveDays).TotalSeconds)
                    };

                var containerResponse =
                    await _db.CreateContainerIfNotExistsAsync(
                        containerProperties,
                        cancellationToken: cancellationToken);

                await SetOfferedThroughputAsync(containerResponse.Container, documentOptions, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "cosmos-collection-setup-failed @{context}", logProperties);
            }
        }

        private async Task SetOfferedThroughputAsync(
            Container container,
            DocumentOptions documentOptions,
            CancellationToken cancellationToken)
        {
            var logProperties = new Dictionary<string, string>
            {
                ["db_name"] = _cosmosOptions.DatabaseName,
                ["collection_name"] = documentOptions.Name
            };

            if (documentOptions.SetOfferedThroughputOnStartup)
            {
                var requestOptions = new RequestOptions();
                var currentThroughputResponse = await container.ReadThroughputAsync(requestOptions, cancellationToken);

                if (currentThroughputResponse == null)
                {
                    _logger.LogError("cosmos-setup-returned-throughput-null {@context}", logProperties);
                    return;
                }

                if (currentThroughputResponse.IsReplacePending == true)
                {
                    _logger.LogInformation("cosmos-setup-throughput-already-updating-no-action-taken {@context}",
                        logProperties);
                    return;
                }

                var currentThoughput = currentThroughputResponse.Resource;
                ThroughputProperties updatedThroughput;
                if (documentOptions.EnableAutoScale)
                {
                    documentOptions.OfferedThroughput =
                        Math.Max(documentOptions.OfferedThroughput, DefaultAutoScaleMaxThroughput);

                    if (currentThoughput.AutoscaleMaxThroughput.HasValue &&
                        currentThoughput.AutoscaleMaxThroughput == documentOptions.OfferedThroughput)
                    {
                        _logger.LogInformation("cosmos-setup-throughput-already-set-no-action-taken {@context}",
                            logProperties);
                        return;
                    }

                    logProperties["auto_scale"] = "true";
                    logProperties["max_throughput"] = documentOptions.OfferedThroughput.ToString();
                    updatedThroughput =
                        ThroughputProperties.CreateAutoscaleThroughput(documentOptions.OfferedThroughput);
                }
                else
                {
                    documentOptions.OfferedThroughput =
                        Math.Max(documentOptions.OfferedThroughput, DefaultFixedOfferedThroughput);

                    if (currentThoughput.Throughput.HasValue &&
                        currentThoughput.Throughput == documentOptions.OfferedThroughput)
                    {
                        _logger.LogInformation("cosmos-setup-throughput-already-set-no-action-taken {@context}",
                            logProperties);
                        return;
                    }

                    logProperties["auto_scale"] = "false";
                    logProperties["fixed_throughput"] = documentOptions.OfferedThroughput.ToString();
                    updatedThroughput = ThroughputProperties.CreateManualThroughput(documentOptions.OfferedThroughput);
                }

                _logger.LogInformation("cosmos-setup-updating-throughput-settings {@context}", logProperties);
                await container.ReplaceThroughputAsync(updatedThroughput, cancellationToken: cancellationToken);
            }
        }
    }
}