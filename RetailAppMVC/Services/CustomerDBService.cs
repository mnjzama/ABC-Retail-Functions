using RetailAppCore.Models;
using RetailAppCore.Services;
using RetailAppMVC.Data;
using Microsoft.EntityFrameworkCore;

namespace RetailAppMVC.Services
{
    public class CustomerDBService
    {
        private readonly CustomerService _customerService; // Table Storage via Function API
        private readonly ApplicationDbContext _dbContext; // Azure SQL Database

        public CustomerDBService(CustomerService customerService, ApplicationDbContext dbContext)
        {
            _customerService = customerService;
            _dbContext = dbContext;
        }

        // Create Customer
        public async Task<Customer?> AddCustomerAsync(Customer customer)
        {
            // Add to Table Storage
            var addedCustomer = await _customerService.AddAsync(customer);

            // Add to SQL Database
            if (addedCustomer != null)
            {
                if (string.IsNullOrEmpty(addedCustomer.PartitionKey)) addedCustomer.PartitionKey = "Customer";
                if (string.IsNullOrEmpty(addedCustomer.RowKey)) addedCustomer.RowKey = Guid.NewGuid().ToString();

                _dbContext.Customers.Add(addedCustomer);
                await _dbContext.SaveChangesAsync();
            }

            return addedCustomer;
        }

        // Get All Customers
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _dbContext.Customers.ToListAsync();
        }

        // Get Customer by PartitionKey and RowKey
        public async Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            return await _customerService.GetAsync(partitionKey, rowKey);
        }

        // Update Customer
        public async Task UpdateCustomerAsync(Customer customer)
        {
            if (!string.IsNullOrEmpty(customer.Password) && customer.Password != customer.ConfirmPassword)
            {
                throw new InvalidOperationException("Password and Confirm Password must match.");
            }

            // Update Table Storage
            await _customerService.UpdateAsync(customer);

            // Update SQL Database
            var existing = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.PartitionKey == customer.PartitionKey && c.RowKey == customer.RowKey);

            if (existing != null)
            {
                existing.FirstName = customer.FirstName;
                existing.LastName = customer.LastName;
                existing.Email = customer.Email;
                existing.PhoneNumber = customer.PhoneNumber;

                if (!string.IsNullOrEmpty(customer.Password))
                {
                    existing.Password = customer.Password; // Save the password in the SQL Database
                    existing.ConfirmPassword = customer.ConfirmPassword;
                }

                await _dbContext.SaveChangesAsync();
            }
            else
            {
                // If customer not found, add as a new customer
                _dbContext.Customers.Add(customer);
                await _dbContext.SaveChangesAsync();
            }
        }

        // Delete Customer
        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            // Delete from Table Storage
            await _customerService.DeleteAsync(partitionKey, rowKey);

            // Delete from SQL Database
            var existing = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.PartitionKey == partitionKey && c.RowKey == rowKey);

            if (existing != null)
            {
                _dbContext.Customers.Remove(existing);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
