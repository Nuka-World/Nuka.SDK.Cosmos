using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nuka.SDK.Cosmos.Services
{
    [ExcludeFromCodeCoverage]
    public class CosmosSetupBackgroundService : BackgroundService
    {
        private readonly ICosmosSetupService _cosmosSetupService;
        private readonly ILogger<CosmosSetupBackgroundService> _logger;

        public CosmosSetupBackgroundService(
            ICosmosSetupService cosmosSetupService,
            ILogger<CosmosSetupBackgroundService> logger)
        {
            _logger = logger;
            _cosmosSetupService = cosmosSetupService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => { _logger.LogWarning("cosmos-setup-background-task-stopping"); });
            await Task.Run(() => _cosmosSetupService.InitializeCosmosDbAsync(stoppingToken), stoppingToken);
            _logger.LogWarning("cosmos-setup-background-task-stopping");
        }
    }
}