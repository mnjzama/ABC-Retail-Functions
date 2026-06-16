using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using RetailAppCore.Models;
using RetailAppCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ABCRetailFunctions
{
    public class BlobFunction
    {
        private readonly ILogger<BlobFunction> _logger;
        private readonly BlobService _blobService;
        private readonly TableClient _tableClient;
        private readonly ProductService _productService;

        private readonly string _storageConnectionString = ""; // Replace with your actual Azure Storage connection string
        private readonly string _tableName = "Products";

        public BlobFunction(ILogger<BlobFunction> logger, ProductService productService)
        {
            _logger = logger;
            _productService = productService;

            // Initialize BlobService and TableClient directly
            _blobService = new BlobService(_storageConnectionString);
            _tableClient = new TableClient(_storageConnectionString, _tableName);
        }

        [Function("UploadProductImage")]
        public async Task<HttpResponseData> UploadProductImage(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload-product-image")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Processing product image upload request.");

                // Check if the content type is present in the headers
                if (!req.Headers.Contains("Content-Type"))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteStringAsync("Content-Type header missing.");
                    return badResp;
                }

                // Extract the multipart form data boundary
                var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();
                var boundary = contentType?.Split("boundary=")[1];

                if (string.IsNullOrEmpty(boundary))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteStringAsync("Boundary parameter missing in Content-Type.");
                    return badResp;
                }

                // Parse the multipart form data
                var multipartReader = new MultipartReader(boundary, req.Body);
                var section = await multipartReader.ReadNextSectionAsync();

                string uploadedBlobUrl = null;
                Product newProduct = new Product();

                while (section != null)
                {
                    var contentDisposition = section.Headers["Content-Disposition"].ToString();
                    var name = contentDisposition.Split(',')[1].Trim().Split('=')[1].Trim('"');

                    if (name == "ProductId")
                    {
                        var value = await new StreamReader(section.Body).ReadToEndAsync();
                        newProduct.ProductId = value;
                    }
                    else if (name == "ProductName")
                    {
                        var value = await new StreamReader(section.Body).ReadToEndAsync();
                        newProduct.ProductName = value;
                    }
                    else if (name == "Image")
                    {
                        var fileName = contentDisposition.Split(';')[2].Trim().Split('=')[1].Trim('"');
                        var uniqueFileName = $"{Guid.NewGuid()}-{Path.GetFileName(fileName)}";

                        // Upload the image to Blob Storage
                        uploadedBlobUrl = await _blobService.UploadAsync(section.Body, uniqueFileName);

                        // Set the ImageUrl of the product
                        newProduct.ImageUrl = uploadedBlobUrl;
                    }

                    section = await multipartReader.ReadNextSectionAsync();
                }

                if (string.IsNullOrEmpty(newProduct.ProductId) || string.IsNullOrEmpty(newProduct.ProductName) || string.IsNullOrEmpty(uploadedBlobUrl))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteStringAsync("Missing product ID, name, or image URL.");
                    return badResp;
                }

                // Save the new product to Table Storage
                await _tableClient.AddEntityAsync(newProduct);
                _logger.LogInformation($"Product created with ProductId: {newProduct.ProductId}, Image URL: {uploadedBlobUrl}");

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteStringAsync($"Product uploaded successfully. Image URL: {uploadedBlobUrl}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image.");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Error uploading product image.");
                return resp;
            }
        }


        [Function("DeleteProductImage")]
        public async Task<HttpResponseData> DeleteProductImage(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "delete-product-image")] HttpRequestData req)
        {
            try
            {
                var productId = req.Query["productId"];
                if (string.IsNullOrEmpty(productId))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteStringAsync("Product ID is missing.");
                    return badResp;
                }

                string partitionKey = "Product";
                var product = await _productService.GetAsync(partitionKey, productId);

                if (product == null || string.IsNullOrEmpty(product.ImageUrl))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.NotFound);
                    await badResp.WriteStringAsync("Product not found or no image associated.");
                    return badResp;
                }

                // Delete the product image from Blob Storage using the ImageUrl
                await _blobService.DeleteBlobAsync(product.ImageUrl);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("Product image deleted successfully.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product image.");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Error deleting product image.");
                return resp;
            }
        }

    }
}