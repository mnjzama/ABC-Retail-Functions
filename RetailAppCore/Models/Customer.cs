using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RetailAppCore.Models
{
    public class Customer : ITableEntity
    {
        public string CustomerId { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "First Name is required.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        public string LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        // For Authentication
        public string Password { get; set; } = string.Empty; 
        public string ConfirmPassword { get; set; } = string.Empty; 

        public bool IsPasswordMatch()
        {
            return Password == ConfirmPassword;
        }

        // Table Storage identifiers
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }

        [NotMapped]
        public ETag ETag { get; set; }
    }
}
