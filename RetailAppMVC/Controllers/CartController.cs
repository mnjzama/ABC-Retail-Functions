using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RetailAppCore.Models;
using RetailAppCore.Services;
using RetailAppCore.ViewModels;
using RetailAppMVC.Helpers;
using RetailAppMVC.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RetailAppMVC.Controllers
{
    public class CartController : Controller
    {
        private readonly ProductService _productService;
        private readonly CustomerService _customerService;
        private readonly OrderDBService _orderDBService;

        public CartController(ProductService productService,
                              CustomerService customerService,
                              OrderDBService orderDBService)
        {
            _productService = productService;
            _customerService = customerService;
            _orderDBService = orderDBService;
        }

        // Helper methods to get and save cart in session
        private List<CartItem> GetCart()
        {
            var cart = HttpContext.Session.GetJson<List<CartItem>>("CART");
            Console.WriteLine($"Cart contains {cart?.Count} items.");

            return cart ?? new List<CartItem>();
        }

        // Save cart to session
        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetJson("CART", cart);
            Console.WriteLine("Cart has been saved.");
        }

        // Display cart contents
        public IActionResult Index()
        {
            var cart = GetCart();
            ViewBag.Total = cart.Sum(c => c.LineTotal);
            return View(cart.OrderBy(c => c.ProductName).ToList());
        }

        // Add item to cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int qty = 1)
        {
            if (string.IsNullOrEmpty(productId) || qty <= 0)
                return RedirectToAction("Index", "Product");

            var product = await _productService.GetAsync("Product", productId);
            if (product is null)
                return NotFound();

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ProductId == productId);

            if (existing is null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.RowKey,
                    ProductName = product.ProductName,
                    UnitPrice = Convert.ToDouble(product.Price),
                    Quantity = qty
                });
            }
            else
            {
                existing = new CartItem
                {
                    ProductId = existing.ProductId,
                    ProductName = existing.ProductName,
                    UnitPrice = existing.UnitPrice,
                    Quantity = existing.Quantity + qty
                };
                cart.RemoveAll(c => c.ProductId == productId);
                cart.Add(existing);
            }

            SaveCart(cart);
            TempData["Message"] = $"<strong>{product.ProductName}</strong> (Qty: {qty}) has been added to your cart";
            return RedirectToAction("Index", "Product");
        }

        // Update item quantity in cart
        [HttpPost]
        public IActionResult Update(string productId, int qty)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item is null) return RedirectToAction(nameof(Index));

            if (qty <= 0)
                cart.Remove(item);
            else
            {
                cart.Remove(item);
                cart.Add(new CartItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = qty
                });
            }
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        // Remove item from cart
        [HttpPost]
        public IActionResult Remove(string productId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.ProductId == productId);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        // Clear the cart
        [HttpPost]
        public IActionResult Clear()
        {
            SaveCart(new List<CartItem>());
            return RedirectToAction(nameof(Index));
        }

        // Checkout - Display checkout form
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            // Ensure the user is logged in before proceeding
            var customerId = User?.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            var customers = await _customerService.GetAllAsync();
            ViewBag.Customers = customers
                .Select(c => new SelectListItem
                {
                    Value = c.RowKey,
                    Text = $"{c.FirstName} {c.LastName}"
                }).ToList();

            var vm = new CheckoutViewModel
            {
                CartItems = cart,
                CustomerId = customerId,
                CustomerName = customers
                    .Where(c => c.RowKey == customerId)
                    .Select(c => $"{c.FirstName} {c.LastName}")
                    .FirstOrDefault() ?? "Valued Customer"

            };
            return View(vm);
        }

        // Checkout - Process checkout form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();
            if (!cart.Any() || string.IsNullOrEmpty(model.CustomerId))
            {
                Console.WriteLine("Error: Cart is empty or CustomerId is missing.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                Console.WriteLine("Checkout process started.");

                // single order number for entire checkout
                var orderNumber = Guid.NewGuid().ToString();

                foreach (var item in cart)
                {
                    var order = new Order
                    {
                        PartitionKey = "Order",
                        RowKey = Guid.NewGuid().ToString(), 
                        OrderNumber = orderNumber,
                        CustomerId = model.CustomerId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        OrderDate = DateTime.UtcNow,
                        Status = "Pending",
                        TotalPrice = item.LineTotal
                    };

                    Console.WriteLine($"Creating order: ProductId={order.ProductId}, Qty={order.Quantity}, OrderNumber={order.OrderNumber}, TotalPrice={order.TotalPrice}");
                    await _orderDBService.CreateOrderAsync(order);
                }

                SaveCart(new List<CartItem>());
                TempData["Message"] = "Your order has been placed successfully! Thank you for shopping with us!";
                Console.WriteLine("Order placed successfully.");

                return RedirectToAction("Index", "Product");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating order: {ex.Message}";
                Console.WriteLine($"Error creating order: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

    }
}