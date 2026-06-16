using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Queues;
using RetailAppCore.Models;

namespace RetailAppCore.Services
{
    public class QueueService
    {
        private readonly QueueClient _queueClient;

        public QueueService(string connectionString, string queueName)
        {
            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists(); // ensure queue exists
        }

        // Send an Order directly
        public async Task SendOrderAsync(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            order.PartitionKey ??= "Order";
            order.RowKey ??= Guid.NewGuid().ToString();
            order.OrderDate = order.OrderDate == default ? DateTime.UtcNow : order.OrderDate;

            // Serialize order to JSON
            var messageJson = JsonSerializer.Serialize(order);

            // Send plain JSON to Azure Queue
            await _queueClient.SendMessageAsync(messageJson);

            Console.WriteLine($"[QueueService] Order sent to queue. RowKey={order.RowKey}");
        }
    }
}