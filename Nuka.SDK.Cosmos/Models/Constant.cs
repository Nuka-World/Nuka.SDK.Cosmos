using System.Diagnostics.CodeAnalysis;

namespace Nuka.SDK.Cosmos.Models
{
    [ExcludeFromCodeCoverage]
    internal class Constant
    {
        public const string CONSISTENCY_LEVEL_STRONG = "Strong";
        public const string CONSISTENCY_LEVEL_BOUNDED_STALENESS = "Bounded_Staleness";
        public const string CONSISTENCY_LEVEL_SESSION = "Session";
        public const string CONSISTENCY_LEVEL_CONSISTENT_PREFIX = "Consistent_Prefix";
        public const string CONSISTENCY_LEVEL_EVENTUAL = "Eventual";
    }
}