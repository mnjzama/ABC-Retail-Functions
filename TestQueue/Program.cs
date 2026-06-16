using Azure.Storage.Queues;
using RetailAppCore.Models;
using System.Net.Http.Json;
using System.Text.Json;

internal class Program
{
    static async Task Main(string[] args)
    {
        static async Task Main(string[] args)
        {
            using var httpClient = new HttpClient();

            // Replace with your actual Function App base URL
            string baseUrl = "";  // Replace with your actual Azure Function API base URL
            string endpoint = $"{baseUrl}/list-files";

            try
            {
                Console.WriteLine($"Calling {endpoint} ...");

                var files = await httpClient.GetFromJsonAsync<List<FileModel>>(endpoint);

                if (files == null || files.Count == 0)
                {
                    Console.WriteLine("No files found.");
                }
                else
                {
                    Console.WriteLine("Files:");
                    foreach (var file in files)
                    {
                        Console.WriteLine($"{file.FileName} ({file.DisplaySize}) - Last modified: {file.LastModified}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
            }
        }
    }
}

