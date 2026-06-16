using Microsoft.AspNetCore.Mvc;
using RetailAppCore.Models;
using RetailAppCore.Services;
using RetailAppMVC.Services;

namespace RetailAppMVC.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CustomerDBService _customerDBService; // Handles both TableStorage + SQL
        private readonly OrderDBService _orderDBService;

        public CustomerController(CustomerDBService customerDBService, OrderDBService orderDBService)
        {
            _customerDBService = customerDBService;
            _orderDBService = orderDBService;
        }

        // Index
        public async Task<IActionResult> Index()
        {
            var customers = await _customerDBService.GetAllCustomersAsync();
            return View(customers);
        }

        // Details
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var customer = await _customerDBService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // Create
        public IActionResult Create()
        {
            return View();
        }

        // Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid) return View(customer);

            // Validate password match
            if (!customer.IsPasswordMatch())
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View(customer);
            }

            customer.PartitionKey ??= "Customer";
            customer.RowKey ??= Guid.NewGuid().ToString();

            try
            {
                await _customerDBService.AddCustomerAsync(customer);
                TempData["Message"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating customer: {ex.Message}";
                return View(customer);
            }
        }

        // Edit
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var customer = await _customerDBService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (!ModelState.IsValid) return View(customer);

            // If passwords are provided, check if they match
            if (!string.IsNullOrEmpty(customer.Password) || !string.IsNullOrEmpty(customer.ConfirmPassword))
            {
                if (!customer.IsPasswordMatch())
                {
                    ModelState.AddModelError(string.Empty, "Passwords do not match.");
                    return View(customer);
                }
            }

            try
            {
                await _customerDBService.UpdateCustomerAsync(customer);
                TempData["Message"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Details), new { partitionKey = customer.PartitionKey, rowKey = customer.RowKey });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating customer: {ex.Message}";
                return View(customer);
            }
        }

        // Delete
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var customer = await _customerDBService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            // Check if any orders exist for this customer
            var orders = await _orderDBService.GetAllOrdersAsync();
            bool hasOrders = orders.Any(o => o.CustomerId == customer.RowKey);

            if (hasOrders)
            {
                TempData["Error"] = "Cannot delete this customer because there are existing orders linked to them.";
                return RedirectToAction("Index");
            }

            return View(customer);
        }

        // DeleteConfirmed
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            try
            {
                await _customerDBService.DeleteCustomerAsync(partitionKey, rowKey);
                TempData["Message"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Search
        [HttpGet]
        public async Task<IActionResult> Search(string query, string filterBy)
        {
            var customers = await _customerDBService.GetAllCustomersAsync();

            if (!string.IsNullOrEmpty(query))
            {
                query = query.ToLower();
                customers = customers.Where(c =>
                    (filterBy == "Name" && ($"{c.FirstName} {c.LastName}".ToLower().Contains(query))) ||
                    (filterBy == "Email" && c.Email.ToLower().Contains(query)) ||
                    (filterBy == "Phone" && c.PhoneNumber.ToLower().Contains(query))
                ).ToList();
            }

            ViewData["FilterBy"] = filterBy;
            ViewData["Query"] = query;
            return View(customers);
        }
    }
}