using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailAppCore.Models
{
    public class Product : ITableEntity
    {
        public string ProductId { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Product Name is required.")]
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public double Price { get; set; }

        // Table Storage identifiers
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        
        [NotMapped]
        public ETag ETag { get; set; }
    }
}
