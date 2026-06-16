using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RetailAppCore.Models;
using RetailAppCore.Services;
using RetailAppCore.ViewModels;
using RetailAppMVC.Services;

namespace RetailAppMVC.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderDBService _orderDBService;
        private readonly CustomerService _customerService;
        private readonly ProductService _productService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            OrderDBService orderDBService,
            CustomerService customerService,
            ProductService productService,
            ILogger<OrderController> logger)
        {
            _orderDBService = orderDBService;
            _customerService = customerService;
            _productService = productService;
            _logger = logger;
        }


        // Index
        public async Task<IActionResult> Index()
        {
            var orders = await _orderDBService.GetAllOrdersAsync();
            var customers = await _customerService.GetAllAsync();
            var products = await _productService.GetAllAsync();

            var orderViewModels = orders.Select(o =>
            {
                var customer = customers.FirstOrDefault(c => c.RowKey == o.CustomerId);
                var product = products.FirstOrDefault(p => p.RowKey == o.ProductId);
                double price = product?.Price ?? 0;

                return new OrderViewModel
                {
                    PartitionKey = o.PartitionKey,
                    RowKey = o.RowKey,
                    OrderNumber = o.OrderNumber, 
                    CustomerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Unknown",
                    ProductName = product?.ProductName ?? "Unknown",
                    Quantity = o.Quantity,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    CustomerId = o.CustomerId,
                    OrdersPrice = o.Quantity * price,
                    ProductId = o.ProductId,
                    Price = price
                };
            }).ToList();

            return View(orderViewModels);
        }


        // Edit
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var order = await _orderDBService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            var customers = await _customerService.GetAllAsync();
            var products = await _productService.GetAllAsync();

            ViewBag.Customers = new SelectList(customers, "RowKey", "FirstName", order.CustomerId);
            ViewBag.Products = new SelectList(products, "RowKey", "ProductName", order.ProductId);

            return View(order);
        }

        // Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (!ModelState.IsValid)
                return View(order);

            try
            {
                await _orderDBService.UpdateOrderAsync(order); // Updates both Table Storage + SQL
                TempData["Message"] = "Order updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                TempData["Error"] = $"Error updating order: {ex.Message}";
                return View(order);
            }
        }

        // Details
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var order = await _orderDBService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            var customer = await _customerService.GetAsync("Customer", order.CustomerId);
            var product = await _productService.GetAsync("Product", order.ProductId);

            var price = product?.Price ?? 0;

            var viewModel = new OrderViewModel
            {
                PartitionKey = order.PartitionKey,
                RowKey = order.RowKey,
                CustomerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Unknown",
                ProductName = product?.ProductName ?? "Unknown",
                Quantity = order.Quantity,
                OrderDate = order.OrderDate,
                Status = order.Status,
                CustomerId = order.CustomerId,
                ProductId = order.ProductId,
                Price = price,
                OrdersPrice = order.Quantity * price,
            };

            return View(viewModel);
        }

        // Delete
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var order = await _orderDBService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            var customer = await _customerService.GetAsync("Customer", order.CustomerId);
            var product = await _productService.GetAsync("Product", order.ProductId);

            var viewModel = new OrderViewModel
            {
                PartitionKey = order.PartitionKey,
                RowKey = order.RowKey,
                CustomerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Unknown",
                ProductName = product?.ProductName ?? "Unknown",
                Quantity = order.Quantity,
                Price = product?.Price ?? 0,
                OrderDate = order.OrderDate,
                Status = order.Status,
                CustomerId = order.CustomerId,
                ProductId = order.ProductId
            };

            return View(viewModel);
        }

        // DeleteConfirmed
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            try
            {
                await _orderDBService.DeleteOrderAsync(partitionKey, rowKey); // Deletes from both Table Storage + SQL
                TempData["Message"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // EditStatus
        public async Task<IActionResult> EditStatus(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return BadRequest("Invalid order keys.");

            var order = await _orderDBService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            var customer = await _customerService.GetAsync("Customer", order.CustomerId);
            var product = await _productService.GetAsync("Product", order.ProductId);

            var viewModel = new OrderViewModel
            {
                PartitionKey = order.PartitionKey,
                RowKey = order.RowKey,
                OrderDate = order.OrderDate,
                Quantity = order.Quantity,
                Status = order.Status,
                CustomerId = order.CustomerId,
                ProductId = order.ProductId,
                CustomerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Unknown",
                ProductName = product?.ProductName ?? "Unknown"
            };

            var statuses = new List<string> { "Pending", "Processing", "Completed", "Cancelled" };
            ViewBag.Statuses = new SelectList(statuses, order.Status);
            ViewBag.PartitionKey = partitionKey;
            ViewBag.RowKey = rowKey;

            return View(viewModel);
        }

        // EditStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStatus(string partitionKey, string rowKey, string status)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return BadRequest("Invalid order keys.");

            var order = await _orderDBService.GetOrderAsync(partitionKey, rowKey);
            if (order == null) return NotFound();

            order.Status = status;
            await _orderDBService.UpdateOrderAsync(order);

            TempData["Message"] = $"Order status updated to {status}!";
            return RedirectToAction(nameof(Index));
        }

        // Search
        [HttpGet]
        public async Task<IActionResult> Search(string query, string filterBy)
        {
            var orders = await _orderDBService.GetAllOrdersAsync();

            // Map to OrderViewModel with Names
            var customers = await _customerService.GetAllAsync();
            var products = await _productService.GetAllAsync();

            var ordersVM = orders.Select(o => new OrderViewModel
            {
                PartitionKey = o.PartitionKey,
                RowKey = o.RowKey,
                CustomerId = o.CustomerId,
                CustomerName = customers.FirstOrDefault(c => c.RowKey == o.CustomerId)?.FirstName + " " +
                               customers.FirstOrDefault(c => c.RowKey == o.CustomerId)?.LastName ?? "Unknown",
                ProductId = o.ProductId,
                ProductName = products.FirstOrDefault(p => p.RowKey == o.ProductId)?.ProductName ?? "Unknown",
                Quantity = o.Quantity,
                Price = products.FirstOrDefault(p => p.RowKey == o.ProductId)?.Price ?? 0,
                Status = o.Status,
                OrderDate = o.OrderDate
            }).ToList();

            if (!string.IsNullOrEmpty(query))
            {
                query = query.ToLower();
                ordersVM = ordersVM.Where(o =>
                    (filterBy == "Customer" && o.CustomerName.ToLower().Contains(query)) ||
                    (filterBy == "Product" && o.ProductName.ToLower().Contains(query)) ||
                    (filterBy == "Status" && o.Status.ToLower().Contains(query))
                ).ToList();
            }

            ViewData["FilterBy"] = filterBy;
            ViewData["Query"] = query;
            return View(ordersVM);
        }

        // My Orders for authenticated customer
        public async Task<IActionResult> MyOrders()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var customerId = User.FindFirst("CustomerId")?.Value;
            if (customerId == null)
                return BadRequest("Customer ID not found");

            // Get all orders for this customer
            var orders = await _orderDBService.GetOrdersByCustomerAsync(customerId);

            var products = await _productService.GetAllAsync();
            var productDict = products.ToDictionary(p => p.RowKey, p => p);

            // Group orders by OrderNumber
            var groupedOrders = orders.GroupBy(o => o.OrderNumber)
                                .Select(g =>
                                {
                                    var firstOrder = g.First();
                                    var vm = new OrderViewModel
                                    {
                                        OrderNumber = firstOrder.OrderNumber,
                                        CustomerId = firstOrder.CustomerId,
                                        CustomerName = g.First().CustomerId,
                                        OrderDate = firstOrder.OrderDate,
                                        Status = firstOrder.Status,
                                        Products = g.Select(o =>
                                        {
                                            productDict.TryGetValue(o.ProductId, out var product);
                                            return new OrderProductViewModel
                                            {
                                                ProductName = product?.ProductName ?? "Unknown",
                                                Quantity = o.Quantity,
                                                Price = product?.Price ?? 0
                                            };
                                        }).ToList()
                                    };
                                    return vm;
                                }).ToList();


            if (!groupedOrders.Any())
                TempData["Info"] = "You have no orders yet.";

            return View(groupedOrders);
        }

    }
}