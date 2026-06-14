using Azure;
using Azure.Data.Tables;

namespace SmartMetering.AzureFunctions.Infrastructure.Persistence
{
    public abstract class BaseTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = default;
    }
}