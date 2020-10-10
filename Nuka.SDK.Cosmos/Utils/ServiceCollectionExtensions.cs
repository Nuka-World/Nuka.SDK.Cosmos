using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nuka.SDK.Cosmos.Options;
using Nuka.SDK.Cosmos.Repositories.Document;
using Nuka.SDK.Cosmos.Services;

namespace Nuka.SDK.Cosmos.Utils
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCosmosDb(
            this IServiceCollection serviceCollection,
            IConfiguration configuration)
        {
            var cosmosOptions = GetAndValidateCosmosOptionConfig(serviceCollection, configuration);

            foreach (var document in cosmosOptions.Documents)
                serviceCollection.AddCosmosRepository(document);

            serviceCollection.AddSingleton(sp =>
                {
                    var clientBuilder = new CosmosClientBuilder(cosmosOptions.EndpointUri, cosmosOptions.AccessKey);
                    clientBuilder = cosmosOptions.DirectConnection
                        ? clientBuilder.WithConnectionModeDirect()
                        : clientBuilder.WithConnectionModeGateway();
                    clientBuilder = clientBuilder
                        .WithThrottlingRetryOptions(TimeSpan.FromSeconds(cosmosOptions.MaxRetryWaitTimeInSeconds), 3)
                        .WithBulkExecution(cosmosOptions.HasBulkExecutionEnabled);
                    return clientBuilder.Build();
                }
            );

            serviceCollection.AddSingleton<IHostedService>(c =>
            {
                var cosmosSetupService = new CosmosSetupService(
                    c.GetRequiredService<CosmosClient>(),
                    cosmosOptions,
                    c.GetRequiredService<ILogger<CosmosSetupService>>());
                return new CosmosSetupBackgroundService(cosmosSetupService,
                    c.GetRequiredService<ILogger<CosmosSetupBackgroundService>>());
            });

            return serviceCollection;
        }

        private static IServiceCollection AddCosmosRepository(
            this IServiceCollection serviceCollection,
            DocumentOptions documentOptions)
        {
            if (documentOptions == null)
                throw new ArgumentNullException(nameof(documentOptions));

            var docTypes =
                from a in AppDomain.CurrentDomain.GetAssemblies()
                from t in a.GetTypes()
                let attributes = t.GetCustomAttributes(typeof(DataContractAttribute), true)
                where attributes != null && attributes.Length > 0
                from dc in attributes
                where (dc as DataContractAttribute)?.Name == documentOptions.DocumentClass
                select t;

            var doc = docTypes.FirstOrDefault();

            if (doc == null)
                throw new Exception(
                    $"AddCosmosRepository: Invalid Document configuration, the document {documentOptions.Name} does not exist");

            var repository = typeof(IDocumentRepository<>).MakeGenericType(doc);

            serviceCollection
                .AddSingleton(repository,
                    sp =>
                    {
                        var cosmosRepoType = typeof(CosmosDocumentRepository<>).MakeGenericType(doc);
                        var loggerType = typeof(ILogger<>).MakeGenericType(cosmosRepoType);
                        object[] repoParams =
                        {
                            sp.GetService<CosmosClient>(),
                            sp.GetService<IOptions<CosmosOptions>>(),
                            documentOptions.Name,
                            documentOptions.PartitionKeyName,
                            sp.GetService(loggerType)
                        };

                        return Activator.CreateInstance(cosmosRepoType, repoParams);
                    }
                );

            return serviceCollection;
        }

        private static CosmosOptions GetAndValidateCosmosOptionConfig(
            IServiceCollection serviceCollection,
            IConfiguration configuration)
        {
            IConfigurationSection cosmosConfig;
            try
            {
                cosmosConfig = configuration.GetSection("CosmosOptions");
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException(nameof(CosmosOptions),
                    "The CosmosOptions section is missing from the service configuration");
            }

            var cosmosOptions = cosmosConfig.Get<CosmosOptions>();

            if (string.IsNullOrWhiteSpace(cosmosOptions.EndpointUri))
                throw new ArgumentNullException(nameof(cosmosOptions.EndpointUri),
                    "The CosmosOptions:EndpointUri parameter is missing from the service configuration");

            if (Uri.TryCreate(cosmosOptions.EndpointUri, UriKind.Absolute, out _) == false)
                throw new ArgumentException(
                    "The CosmosOptions:EndpointUri parameter must be a properly formatted URI",
                    nameof(cosmosOptions.EndpointUri));

            if (string.IsNullOrWhiteSpace(cosmosOptions.DatabaseName))
                throw new ArgumentNullException(nameof(cosmosOptions.DatabaseName),
                    "The CosmosOptions:DatabaseName parameter is missing from the service configuration");

            if (string.IsNullOrWhiteSpace(cosmosOptions.AccessKey))
            {
                throw new ArgumentNullException(nameof(cosmosOptions.AccessKey),
                    "The CosmosOptions:AccessKey parameter is missing from the service configuration");
            }

            if (cosmosOptions.Documents == null || cosmosOptions.Documents.Length == 0)
            {
                throw new ArgumentNullException(nameof(cosmosOptions.Documents),
                    "The CosmosOptions:Documents collection is missing or empty");
            }

            serviceCollection.Configure<CosmosOptions>(cosmosConfig);
            return cosmosOptions;
        }
    }
}