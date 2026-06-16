using Microsoft.EntityFrameworkCore;
using RetailAppCore.Models;
using RetailAppCore.Services;
using RetailAppCore.ViewModels;
using RetailAppMVC.Data;

namespace RetailAppMVC.Services
{
    public class OrderDBService
    {
        private readonly OrderService _orderService; // Table Storage via Function API
        private readonly ApplicationDbContext _dbContext; // Azure SQL Database

        public OrderDBService(OrderService orderService, ApplicationDbContext dbContext)
        {
            _orderService = orderService;
            _dbContext = dbContext;
        }

        // Create Order
        public async Task<string> CreateOrderAsync(Order order)
        {
            // Send order to Table Storage (Function API)
            var responseMessage = await _orderService.CreateAsync(order);

            // Add to SQL Database
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            return responseMessage;
        }

        // Get All Orders
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _dbContext.Orders.ToListAsync();
        }

        // Get Order by PartitionKey and RowKey
        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            return await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.PartitionKey == partitionKey && o.RowKey == rowKey);
        }

        // Update Order
        public async Task UpdateOrderAsync(Order order)
        {
            // Update in Table Storage
            await _orderService.UpdateAsync(order);

            // Update in SQL Database
            var existing = await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.PartitionKey == order.PartitionKey && o.RowKey == order.RowKey);

            if (existing != null)
            {
                existing.CustomerId = order.CustomerId;
                existing.ProductId = order.ProductId;
                existing.Quantity = order.Quantity;
                existing.OrderDate = order.OrderDate;
                existing.Status = order.Status;

                _dbContext.Orders.Update(existing);
                await _dbContext.SaveChangesAsync();
            }
        }

        // Delete Order
        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            // Delete from Table Storage
            await _orderService.DeleteAsync(partitionKey, rowKey);

            // Delete from SQL Database
            var existing = await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.PartitionKey == partitionKey && o.RowKey == rowKey);

            if (existing != null)
            {
                _dbContext.Orders.Remove(existing);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<Order>> GetOrdersByCustomerAsync(string customerId)
        {
            return await _dbContext.Orders
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();
        }
    }
}