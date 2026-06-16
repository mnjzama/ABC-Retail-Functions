using RetailAppCore.Models;
using System.Net.Http.Json;

namespace RetailAppCore.Services
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;

        public ProductService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Product>>("products") ?? new List<Product>();
        }

        public async Task<Product?> GetAsync(string partitionKey, string rowKey)
        {
            return await _httpClient.GetFromJsonAsync<Product>($"products/{partitionKey}/{rowKey}");
        }

        public async Task<Product?> AddAsync(Product product)
        {
            var response = await _httpClient.PostAsJsonAsync("products", product);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Product>();
        }

        public async Task UpdateAsync(Product product)
        {
            var response = await _httpClient.PutAsJsonAsync("products", product);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            var response = await _httpClient.DeleteAsync($"products/{partitionKey}/{rowKey}");
            response.EnsureSuccessStatusCode();
        }
    }
}