using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RetailAppCore.Models;
using RetailAppCore.Services;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ABCRetailFunctions
{
    public class QueueOrderProcessor
    {
        private readonly ILogger<QueueOrderProcessor> _logger;
        private readonly HttpClient _httpClient;
        private TableStorage _tableStorage;

        public QueueOrderProcessor(HttpClient httpClient, TableStorage tableStorage, ILogger<QueueOrderProcessor> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tableStorage = tableStorage ?? throw new ArgumentNullException(nameof(tableStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function(nameof(QueueOrderProcessor))]
        public async Task ProcessQueueMessage(
        [QueueTrigger("queueitems", Connection = "AzureWebJobsStorage")] string queueMessage,
        CancellationToken cancellationToken)
        {
            _logger.LogInformation("[QueueProcessor] Raw message received.");

            if (!TryDeserializeOrder(queueMessage, out var order))
            {
                _logger.LogWarning("[QueueProcessor] Invalid JSON. Skipping message.");
                return;
            }

            if (!ValidateOrder(order))
            {
                _logger.LogWarning("[QueueProcessor] Missing fields. Skipping. RowKey={RowKey}", order.RowKey ?? "N/A");
                return;
            }

            if (order.Processed)
            {
                _logger.LogInformation("[QueueProcessor] Order already processed. RowKey={RowKey}", order.RowKey);
                return;
            }

            order.PartitionKey ??= "Order";
            order.RowKey ??= Guid.NewGuid().ToString();
            order.OrderDate = order.OrderDate == default ? DateTime.UtcNow : order.OrderDate;
            order.Processed = true;

            try
            {
                // Send to HTTP function
                _logger.LogInformation("[QueueProcessor] Sending order to HTTP function. RowKey={RowKey}", order.RowKey);
                var response = await _httpClient.PostAsJsonAsync("orders", order, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[QueueProcessor] HTTP function returned {Status}. Skipping message.", response.StatusCode);
                    return; // don't throw, just skip
                }

                await _tableStorage.AddOrderAsync(order);
                _logger.LogInformation("[QueueProcessor] Order persisted successfully. RowKey={RowKey}", order.RowKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[QueueProcessor] Unexpected error. Message will retry.");
                throw;
            }
        }

        private bool TryDeserializeOrder(string input, out Order? order)
        {
            order = null;
            try
            {
                order = JsonSerializer.Deserialize<Order>(input);
                if (order != null) return true;
            }
            catch (JsonException) { }

            return false;
        }

        private static bool ValidateOrder(Order order)
        {
            return !string.IsNullOrEmpty(order.CustomerId)
                && !string.IsNullOrEmpty(order.ProductId)
                && order.Quantity > 0;
        }
    }
}