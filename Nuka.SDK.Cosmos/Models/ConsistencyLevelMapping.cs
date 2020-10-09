using Microsoft.Azure.Cosmos;

namespace Nuka.SDK.Cosmos.Models
{
    internal static class ConsistencyLevelMapping
    {
        public static ConsistencyLevel? GetConsistencyLevel(string consistencyLevel)
        {
            ConsistencyLevel? result;

            if (string.IsNullOrEmpty(consistencyLevel))
                return null;

            switch (consistencyLevel)
            {
                case Constant.CONSISTENCY_LEVEL_STRONG:
                    result = ConsistencyLevel.Strong;
                    break;
                case Constant.CONSISTENCY_LEVEL_BOUNDED_STALENESS:
                    result = ConsistencyLevel.BoundedStaleness;
                    break;
                case Constant.CONSISTENCY_LEVEL_SESSION:
                    result = ConsistencyLevel.Session;
                    break;
                case Constant.CONSISTENCY_LEVEL_CONSISTENT_PREFIX:
                    result = ConsistencyLevel.ConsistentPrefix;
                    break;
                case Constant.CONSISTENCY_LEVEL_EVENTUAL:
                    result = ConsistencyLevel.Eventual;
                    break;
                default:
                    result = null;
                    break;
            }
            return result;
        }
    }
}