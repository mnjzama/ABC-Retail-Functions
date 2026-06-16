using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailAppCore.Models;
using RetailAppCore.Services;
using RetailAppMVC.Services;

namespace RetailAppMVC.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductService _productService;
        private readonly ProductDBService _productDBService;
        private readonly OrderDBService _orderDBService;
        private readonly BlobService _blobService; 

        public ProductController(ProductService productService, ProductDBService productDBService, OrderDBService orderDBService, BlobService blobService)
        {
            _productService = productService;
            _productDBService = productDBService;
            _orderDBService = orderDBService;
            _blobService = blobService;
        }

        // Index - List all products
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var products = await _productDBService.GetAllProductsAsync();
            return View(products);
        }

        // Details - View product details
        [Authorize]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var product = await _productDBService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
                return View(product);

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}-{Path.GetFileName(imageFile.FileName)}";
                product.ImageUrl = await _blobService.UploadAsync(imageFile.OpenReadStream(), fileName);
            }

            product.PartitionKey ??= "Product";
            product.RowKey ??= Guid.NewGuid().ToString();

            try
            {
                await _productDBService.AddProductAsync(product); // Adds to both TableStorage + SQL
                TempData["Message"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating product: {ex.Message}";
                return View(product);
            }
        }

        // Edit
        [Authorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var product = await _productDBService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // Edit
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
                return View(product);

            try
            {
                // Upload new image if provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        await _blobService.DeleteBlobAsync(product.ImageUrl);
                    }

                    var fileName = $"{Guid.NewGuid()}-{Path.GetFileName(imageFile.FileName)}";
                    product.ImageUrl = await _blobService.UploadAsync(imageFile.OpenReadStream(), fileName);
                }

                await _productDBService.UpdateProductAsync(product); // Updates both
                TempData["Message"] = "Product updated successfully!";
                return RedirectToAction(nameof(Details), new { partitionKey = product.PartitionKey, rowKey = product.RowKey });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating product: {ex.Message}";
                return View(product);
            }
        }

        // Delete
        [Authorize]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var product = await _productDBService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }

            // Check if any orders exist for this product
            var orders = await _orderDBService.GetAllOrdersAsync();
            bool hasOrders = orders.Any(o => o.ProductId == product.RowKey);

            if (hasOrders)
            {
                TempData["Error"] = "Cannot delete this product because there are existing orders linked to it.";
                return RedirectToAction("Index");
            }
            return View(product);
        }

        // DeleteConfirmed
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            try
            {
                var product = await _productDBService.GetProductAsync(partitionKey, rowKey);
                if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
                {
                    await _blobService.DeleteBlobAsync(product.ImageUrl);
                }

                await _productDBService.DeleteProductAsync(partitionKey, rowKey);
                TempData["Message"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Search
        [HttpGet]
        public async Task<IActionResult> Search(string query, string filterBy, double? minPrice, double? maxPrice)
        {
            var products = await _productService.GetAllAsync();

            if (!string.IsNullOrEmpty(query))
            {
                query = query.ToLower();
                products = products.Where(p =>
                    (filterBy == "Name" && p.ProductName.ToLower().Contains(query)) ||
                    (filterBy == "Description" && p.Description.ToLower().Contains(query))
                ).ToList();
            }

            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice.Value).ToList();

            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice.Value).ToList();

            ViewData["FilterBy"] = filterBy;
            ViewData["Query"] = query;
            ViewData["MinPrice"] = minPrice;
            ViewData["MaxPrice"] = maxPrice;

            return View(products);
        }
    }
}