using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RetailAppCore.Models;
using RetailAppCore.ViewModels;
using RetailAppMVC.Services;
using System.Security.Claims;

namespace RetailAppMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly CustomerDBService _customerDBService;

        public AccountController(CustomerDBService customerDBService)
        {
            _customerDBService = customerDBService;
        }

        public IActionResult LoginChoice()
        {
            return View();
        }

        // AdminLogin
        public IActionResult AdminLogin()
        {
            return View();
        }

        // AdminLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(string name, string password)
        {
            if (name == "Admin" && password == "admin123")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Admin"),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["Message"] = "👨‍💼 Welcome, Admin! You now have full access to the system.";
                return RedirectToAction("Index", "Customer");
            }

            TempData["Error"] = "❌ Invalid admin credentials. Please try again.";
            return View();
        }

        // Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Password and Confirm Password must match.");
                return View(model); 
            }

            // Check if the email already exists
            var existingCustomer = await _customerDBService.GetAllCustomersAsync();
            if (existingCustomer.Any(c => c.Email == model.Email))
            {
                ModelState.AddModelError("Email", "A customer with this email already exists.");
                return View(model);
            }

            var customer = new Customer
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = "Customer"
            };


            var addedCustomer = await _customerDBService.AddCustomerAsync(customer);

            if (addedCustomer != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, customer.FirstName),
                    new Claim(ClaimTypes.Email, customer.Email),
                    new Claim("CustomerId", customer.RowKey)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["Message"] = "Welcome, " + customer.FirstName + "! You have successfully registered. Start shopping now!";
                return RedirectToAction("Index", "Product");
            }

            TempData["Error"] = "There was an error during registration.";
            return RedirectToAction("Index", "Home");
        }


        // Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customers = await _customerDBService.GetAllCustomersAsync();

            var validCustomer = customers.FirstOrDefault(c =>
                c.FirstName == model.FirstName &&
                c.LastName == model.LastName &&
                c.Password == model.Password);

            if (validCustomer != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, validCustomer.FirstName),
                    new Claim(ClaimTypes.Email, validCustomer.Email),
                    new Claim("CustomerId", validCustomer.RowKey) 
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["Message"] = "Welcome back, " + validCustomer.FirstName + "! Your login was successful. Start shopping now!";

                return RedirectToAction("Index", "Product");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid credentials");

                TempData["Error"] = "We couldn't find a matching account. Please make sure your details are correct and try again!";

                return View(model);
            }
        }

        // ConfirmLogout
        public IActionResult ConfirmLogout()
        {
            return View();
        }


        // Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Message"] = "You have successfully logged out!";

            return RedirectToAction("LoginChoice", "Account");
        }
    }
}