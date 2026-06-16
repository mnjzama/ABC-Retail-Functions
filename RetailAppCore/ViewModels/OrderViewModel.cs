using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailAppCore.ViewModels
{
    public class OrderViewModel
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public string CustomerId { get; set; }
        public string CustomerName { get; set; }

        public string ProductId { get; set; }
        public string ProductName { get; set; }

        public int Quantity { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string OrderNumber { get; set; }

        public List<OrderProductViewModel> Products { get; set; } = new();
        public double TotalPrice => Products.Sum(p => p.LineTotal);
        public double OrdersPrice { get; set; }

        public double Price { get; set; }
        public string Status { get; set; }
    }
}
