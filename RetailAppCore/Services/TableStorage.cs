using RetailAppCore.Models;
using Azure;
using Azure.Data.Tables;

namespace RetailAppCore.Services
{
    public class TableStorage
    {
        private readonly TableClient _customerTableClient;
        private readonly TableClient _productTableClient;
        private readonly TableClient _orderTableClient;

        public TableStorage()
        {
            var connectionString = ""; // Replace with your actual Azure Table Storage connection string

            _customerTableClient = new TableClient(connectionString, "Customer");
            _productTableClient = new TableClient(connectionString, "Product");
            _orderTableClient = new TableClient(connectionString, "Order");

            // Ensure tables exist
            _customerTableClient.CreateIfNotExists();
            _productTableClient.CreateIfNotExists();
            _orderTableClient.CreateIfNotExists();
        }


        // Customers
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();
            await foreach (var customer in _customerTableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            if (string.IsNullOrEmpty(customer.PartitionKey)) customer.PartitionKey = "Customer";
            if (string.IsNullOrEmpty(customer.RowKey)) customer.RowKey = Guid.NewGuid().ToString();

            await _customerTableClient.AddEntityAsync(customer);
        }

        public async Task<Customer> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _customerTableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await _customerTableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _customerTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // Products
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var product in _productTableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }
            return products;
        }

        public async Task AddProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.PartitionKey)) product.PartitionKey = "Product";
            if (string.IsNullOrEmpty(product.RowKey)) product.RowKey = Guid.NewGuid().ToString();

            await _productTableClient.AddEntityAsync(product);
        }

        public async Task<Product> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }
        public async Task UpdateProductAsync(Product product)
        {
            await _productTableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // Orders

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();
            await foreach (var order in _orderTableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }
            return orders;
        }

        public async Task<int> GetNextOrderIdAsync()
        {
            var orders = await GetAllOrdersAsync();
            return orders.Any() ? orders.Max(o => o.OrderId) + 1 : 1;
        }

        public async Task AddOrderAsync(Order order)
        {
            if (string.IsNullOrEmpty(order.PartitionKey)) order.PartitionKey = "Order";
            if (string.IsNullOrEmpty(order.RowKey))
                throw new InvalidOperationException("RowKey must be set before saving.");

            await _orderTableClient.UpsertEntityAsync(order, TableUpdateMode.Merge);
        }

        public async Task<Order> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _orderTableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }

        public async Task UpdateOrderAsync(Order order)
        {
            await _orderTableClient.UpdateEntityAsync(order, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            await _orderTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}