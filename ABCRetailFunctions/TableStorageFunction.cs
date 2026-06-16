using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetailAppCore.Models;
using RetailAppCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ABCRetailFunctions
{
    public class TableStorageFunctions
    {
        private readonly TableStorage _tableStorage;
        private readonly ILogger<TableStorageFunctions> _logger;

        private readonly string _storageConnectionString = ""; // Replace with your actual Azure Storage connection string

        public TableStorageFunctions(ILogger<TableStorageFunctions> logger)
        {
            _logger = logger;
            _tableStorage = new TableStorage();
        }

        // Orders

        [Function("GetAllOrders")]
        public async Task<HttpResponseData> GetAllOrders(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
        {
            var orders = await _tableStorage.GetAllOrdersAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(orders);
            return response;
        }

        [Function("AddOrder")]
        public async Task<HttpResponseData> AddOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            try
            {
                var order = await req.ReadFromJsonAsync<Order>();
                if (order == null)
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteStringAsync("Invalid order data.");
                    return badResp;
                }

                order.PartitionKey ??= "Order";
                order.RowKey ??= Guid.NewGuid().ToString();
                order.OrderDate = DateTime.UtcNow;

                // Save to Table Storage
                order.OrderId = await _tableStorage.GetNextOrderIdAsync();
                await _tableStorage.AddOrderAsync(order);

                var queueService = req.FunctionContext.InstanceServices.GetRequiredService<QueueService>();
                await queueService.SendOrderAsync(order);

                var response = req.CreateResponse(HttpStatusCode.Accepted);
                await response.WriteAsJsonAsync(new { message = "Order saved & enqueued", order.RowKey });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save/enqueue order");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Failed to save order.");
                return resp;
            }
        }

        [Function("GetOrder")]
        public async Task<HttpResponseData> GetOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            var order = await _tableStorage.GetOrderAsync(partitionKey, rowKey);
            var response = req.CreateResponse(order == null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
            await response.WriteAsJsonAsync(order);
            return response;
        }

        [Function("UpdateOrder")]
        public async Task<HttpResponseData> UpdateOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "orders")] HttpRequestData req)
        {
            var order = await req.ReadFromJsonAsync<Order>();
            if (order == null)
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteStringAsync("Invalid order data.");
                return badResp;
            }

            await _tableStorage.UpdateOrderAsync(order);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(order);
            return response;
        }

        [Function("DeleteOrder")]
        public async Task<HttpResponseData> DeleteOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "orders/{partitionKey}/{rowKey}")] HttpRequestData req,
        string partitionKey,
        string rowKey)
        {
            try
            {
                await _tableStorage.DeleteOrderAsync(partitionKey, rowKey);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Order {rowKey} deleted successfully.");
                return response;
            }
            catch (Exception ex)
            {
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteStringAsync($"Error deleting order: {ex.Message}");
                return response;
            }
        }


        // Customers

        [Function("GetAllCustomers")]
        public async Task<HttpResponseData> GetAllCustomers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequestData req)
        {
            var customers = await _tableStorage.GetAllCustomersAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(customers);
            return response;
        }

        [Function("AddCustomer")]
        public async Task<HttpResponseData> AddCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")] HttpRequestData req)
        {
            var customer = await req.ReadFromJsonAsync<Customer>();
            if (customer == null)
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteStringAsync("Invalid customer data.");
                return badResp;
            }

            customer.PartitionKey ??= "Customer";
            customer.RowKey ??= Guid.NewGuid().ToString();

            await _tableStorage.AddCustomerAsync(customer);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(customer);
            return response;
        }

        [Function("GetCustomer")]
        public async Task<HttpResponseData> GetCustomer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{partitionKey}/{rowKey}")] HttpRequestData req,
        string partitionKey,
        string rowKey)
        {
            var customer = await _tableStorage.GetCustomerAsync(partitionKey, rowKey);

            var response = req.CreateResponse(customer == null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
            await response.WriteAsJsonAsync(customer);
            return response;
        }


        [Function("UpdateCustomer")]
        public async Task<HttpResponseData> UpdateCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customers")] HttpRequestData req)
        {
            var customer = await req.ReadFromJsonAsync<Customer>();
            if (customer == null)
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteStringAsync("Invalid customer data.");
                return badResp;
            }

            await _tableStorage.UpdateCustomerAsync(customer);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(customer);
            return response;
        }

        [Function("DeleteCustomer")]
        public async Task<HttpResponseData> DeleteCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customers/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            await _tableStorage.DeleteCustomerAsync(partitionKey, rowKey);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Customer {rowKey} deleted successfully.");
            return response;
        }


        // Products

        [Function("GetAllProducts")]
        public async Task<HttpResponseData> GetAllProducts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
        {
            var products = await _tableStorage.GetAllProductsAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(products);
            return response;
        }

        [Function("AddProduct")]
        public async Task<HttpResponseData> AddProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req)
        {
            var product = await req.ReadFromJsonAsync<Product>();
            if (product == null)
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteStringAsync("Invalid product data.");
                return badResp;
            }

            product.PartitionKey ??= "Product";
            product.RowKey ??= Guid.NewGuid().ToString();

            await _tableStorage.AddProductAsync(product);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(product);
            return response;
        }

        [Function("GetProduct")]
        public async Task<HttpResponseData> GetProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            var product = await _tableStorage.GetProductAsync(partitionKey, rowKey);
            var response = req.CreateResponse(product == null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
            await response.WriteAsJsonAsync(product);
            return response;
        }

        [Function("UpdateProduct")]
        public async Task<HttpResponseData> UpdateProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products")] HttpRequestData req)
        {
            var product = await req.ReadFromJsonAsync<Product>();
            if (product == null)
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteStringAsync("Invalid product data.");
                return badResp;
            }

            await _tableStorage.UpdateProductAsync(product);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(product);
            return response;
        }

        [Function("DeleteProduct")]
        public async Task<HttpResponseData> DeleteProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "products/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            await _tableStorage.DeleteProductAsync(partitionKey, rowKey);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Product {rowKey} deleted successfully.");
            return response;
        }
    }
}