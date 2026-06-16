using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailAppCore.Models
{
    public class Order : ITableEntity
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "CustomerId is required.")]
        public string CustomerId { get; set; }

        [Required(ErrorMessage = "ProductId is required.")]
        public string ProductId { get; set; } 

        [Required(ErrorMessage = "Date is required.")]
        public DateTime OrderDate { get; set; }  = DateTime.UtcNow;

        [Required(ErrorMessage = "Quantity is required.")]
        public int Quantity { get; set; }

        public string Status { get; set; } = "Pending";

        public bool Processed { get; set; } = false;

        public double TotalPrice { get; set; }

        public string OrderNumber { get; set; } = Guid.NewGuid().ToString();

        // Table Storage identifiers
        public string PartitionKey { get; set; } = "Order"; 
        public string RowKey { get; set; } = Guid.NewGuid().ToString(); 
        public DateTimeOffset? Timestamp { get; set; }

        [NotMapped]
        public ETag ETag { get; set; }
    }
}