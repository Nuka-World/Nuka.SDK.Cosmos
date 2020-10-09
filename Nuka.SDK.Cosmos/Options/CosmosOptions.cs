using System.Diagnostics.CodeAnalysis;

namespace Nuka.SDK.Cosmos.Options
{
    [ExcludeFromCodeCoverage]
    public class CosmosOptions
    {
        public string AccessKey { get; set; }
        public string DatabaseName { get; set; }
        public string EndpointUri { get; set; }
        public string ConsistencyLevel { get; set; }
        public bool DirectConnection { get; set; }
        public bool HasBulkExecutionEnabled { get; set; }
        public bool EnableSoftDelete { get; set; }
        public int SoftDeleteExpiry { get; set; } = 20;
        public int MaxRetryWaitTimeInSeconds { get; set; } = 30;
        public DocumentOptions[] Documents { get; set; }

    }

    [ExcludeFromCodeCoverage]
    public class DocumentOptions
    {
        public string Name { get; set; }
        public int TimeToLiveDays { get; set; }
        public string PartitionKeyName { get; set; }
        public string DocumentClass { get; set; }
        public int OfferedThroughput { get; set; }
        public bool SetOfferedThroughputOnStartup { get; set; } = true;
        public bool EnableAutoScale { get; set; }
    }
}