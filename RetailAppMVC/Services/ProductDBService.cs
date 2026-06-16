using RetailAppCore.Models;
using RetailAppCore.Services;
using RetailAppMVC.Data;
using Microsoft.EntityFrameworkCore;

namespace RetailAppMVC.Services
{
    public class ProductDBService
    {
        private readonly ProductService _productService; // For Table Storage (via Function API)
        private readonly ApplicationDbContext _dbContext; // For Azure SQL Database

        public ProductDBService(ProductService productService, ApplicationDbContext dbContext)
        {
            _productService = productService;
            _dbContext = dbContext;
        }

        // Create Product
        public async Task<Product?> AddProductAsync(Product product)
        {
            // Add to Table Storage via API
            var addedProduct = await _productService.AddAsync(product);

            // Add to SQL Database if successful
            if (addedProduct != null)
            {
                _dbContext.Products.Add(addedProduct);
                await _dbContext.SaveChangesAsync();
            }

            return addedProduct;
        }

        // Get All Products
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _dbContext.Products.ToListAsync();
        }

        // Get Product by PartitionKey and RowKey
        public async Task<Product?> GetProductAsync(string partitionKey, string rowKey)
        {
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.PartitionKey == partitionKey && p.RowKey == rowKey);

            if (product == null)
            {
                product = await _productService.GetAsync(partitionKey, rowKey);
            }

            return product;
        }

        // Update Product
        public async Task UpdateProductAsync(Product product)
        {
            // Update in Table Storage
            await _productService.UpdateAsync(product);

            // Update in SQL Database
            var existing = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.PartitionKey == product.PartitionKey && p.RowKey == product.RowKey);

            if (existing != null)
            {
                // Update only the fields that can change
                existing.ProductName = product.ProductName;
                existing.Description = product.Description;
                existing.Price = product.Price;

                // Update ImageUrl only if there’s a new image
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    existing.ImageUrl = product.ImageUrl;
                }

                await _dbContext.SaveChangesAsync();
            }
        }

        // Delete Product
        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            // Delete from Table Storage
            await _productService.DeleteAsync(partitionKey, rowKey);

            // Delete from SQL Database
            var existing = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.PartitionKey == partitionKey && p.RowKey == rowKey);

            if (existing != null)
            {
                _dbContext.Products.Remove(existing);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
