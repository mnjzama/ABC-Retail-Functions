using Microsoft.Extensions.Logging;
using RetailAppCore.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RetailAppCore.Services
{
    public class FileService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FileService> _logger;

        public FileService(HttpClient httpClient, ILogger<FileService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<FileModel>> ListAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<FileModel>>("list-files");
                return response ?? new List<FileModel>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Error fetching file list: " + ex.Message);
                throw;  // Rethrow to let the controller handle the error
            }
        }


        public class UploadResult
        {
            public string Filename { get; set; } = "";
            public string Message { get; set; } = "";
        }

        public async Task UploadAsync(MultipartFormDataContent content)
        {
            var response = await _httpClient.PostAsync("upload-file", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Upload failed: {response.StatusCode}, {responseText}");

            Console.WriteLine(responseText);
        }


        public async Task<byte[]> DownloadAsync(string fileName)
        {
            var response = await _httpClient.GetAsync($"download-file?fileName={Uri.EscapeDataString(fileName)}", HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Download failed: {response.StatusCode}, {error}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task DeleteAsync(string fileName)
        {
            var response = await _httpClient.DeleteAsync($"delete-file?filename={fileName}");
            response.EnsureSuccessStatusCode();
        }
    }
}

