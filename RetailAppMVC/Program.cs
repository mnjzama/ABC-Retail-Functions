using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RetailAppCore.Services;
using RetailAppMVC.Data;
using RetailAppMVC.Services;

namespace RetailAppMVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            // Get the connection string from the appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Register DbContext to use SQL Server
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


            var functionApiBaseUrl = ""; // Replace with your actual Azure Function API base URL 
            var azureConnectionString = ""; // Replace with your actual Azure Storage connection string
            var queueName = ""; // Replace with your actual Azure Queue name

            // Register HttpClients for API calls
            builder.Services.AddHttpClient<CustomerService>(c =>
                c.BaseAddress = new Uri(functionApiBaseUrl));
            builder.Services.AddHttpClient<ProductService>(c =>
                c.BaseAddress = new Uri(functionApiBaseUrl));
            builder.Services.AddHttpClient<OrderService>(c =>
                c.BaseAddress = new Uri(functionApiBaseUrl));
            builder.Services.AddHttpClient<FileService>(c =>
                c.BaseAddress = new Uri(functionApiBaseUrl));

            // Register QueueService so MVC can send orders directly to Azure Queue
            builder.Services.AddSingleton(new QueueService(azureConnectionString, queueName));

            // Register BlobService with the connection string
            builder.Services.AddSingleton(new BlobService(azureConnectionString));

            // Register DBServices for combined SQL + TableStorage CRUD
            builder.Services.AddScoped<CustomerDBService>();
            builder.Services.AddScoped<ProductDBService>();
            builder.Services.AddScoped<OrderDBService>();

            // Enable Sessions
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = ".RetailApp.Session";
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
            });

            // Add Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.Cookie.Name = "customer_auth";
                });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Use Sessions
            app.UseSession();

            // Enable Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
