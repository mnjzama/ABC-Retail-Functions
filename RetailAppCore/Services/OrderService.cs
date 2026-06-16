using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RetailAppCore.Models;

namespace RetailAppCore.Services
{
    public class OrderService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderService> _logger;

        public OrderService(HttpClient httpClient, ILogger<OrderService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // Get all orders for MVC view
        public async Task<List<Order>> GetAllAsync()
        {
            // Fetch all orders
            var orders = await _httpClient.GetFromJsonAsync<List<Order>>("orders");

            return orders ?? new List<Order>();
        }

        // Get a single order by its partitionKey and rowKey
        public async Task<Order?> GetAsync(string partitionKey, string rowKey)
        {
            return await _httpClient.GetFromJsonAsync<Order>($"orders/{partitionKey}/{rowKey}");
        }

        // Create a new order
        public async Task<string> CreateAsync(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            // Send order to Function API (which enqueues it)
            var response = await _httpClient.PostAsJsonAsync("orders", order);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(); 
        }

        // Update an existing order
        public async Task UpdateAsync(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            try
            {
                order.OrderDate = order.OrderDate.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc)
                    : order.OrderDate.ToUniversalTime();

                // Log the request being sent
                _logger.LogInformation("Sending PUT request to update order with RowKey: {RowKey}", order.RowKey);

                var response = await _httpClient.PutAsJsonAsync("orders", order);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Order with RowKey: {RowKey} successfully updated.", order.RowKey);
                }
                else
                {
                    _logger.LogWarning("Failed to update order with RowKey: {RowKey}. StatusCode: {StatusCode}", order.RowKey, response.StatusCode);
                    throw new Exception($"Failed to update order. Status Code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating order with RowKey: {RowKey}", order.RowKey);
                throw;
            }
        }

        // Delete an order by its partitionKey and rowKey
        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            var response = await _httpClient.DeleteAsync($"orders/{partitionKey}/{rowKey}");

            // Ensure the request was successful
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to delete order: {response.StatusCode}");
            }
        }
    }
}