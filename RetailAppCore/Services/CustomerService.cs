using RetailAppCore.Models;
using System.Net.Http.Json;

namespace RetailAppCore.Services
{
    public class CustomerService
    {
        private readonly HttpClient _httpClient;

        public CustomerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Customer>>("customers") ?? new List<Customer>();
        }

        public async Task<Customer?> GetAsync(string partitionKey, string rowKey)
        {
            var url = $"customers/{partitionKey}/{rowKey}";

            var response = await _httpClient.GetFromJsonAsync<Customer>(url);
            return response; // This will be null if not found
        }


        public async Task<Customer?> AddAsync(Customer customer)
        {
            var response = await _httpClient.PostAsJsonAsync("customers", customer);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Customer>();
        }

        public async Task UpdateAsync(Customer customer)
        {
            var response = await _httpClient.PutAsJsonAsync("customers", customer);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            var response = await _httpClient.DeleteAsync($"customers/{partitionKey}/{rowKey}");
            response.EnsureSuccessStatusCode();
        }
    }
}
