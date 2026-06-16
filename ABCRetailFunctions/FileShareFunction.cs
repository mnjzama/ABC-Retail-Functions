using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using RetailAppCore.Services;

namespace ABCRetailFunctions
{
    public class FileShareFunction
    {
        private readonly FileShareService _fileShareService;
        private readonly ILogger<FileShareFunction> _logger;

        private readonly string _storageConnectionString = ""; // Replace with your actual Azure Storage connection string
        private readonly string _filePath = ""; // root of uploads share
        private readonly string _fileShareName = ""; // Replace with your actual Azure File Share name

        public FileShareFunction(ILogger<FileShareFunction> logger)
        {
            _logger = logger;
            _fileShareService = new FileShareService(_storageConnectionString, _fileShareName);
        }

        [Function("UploadFile")]
        public async Task<HttpResponseData> UploadFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload-file")] HttpRequestData req)
        {
            try
            {
                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteStringAsync("Missing Content-Type header.");
                    return badResp;
                }

                var contentType = contentTypeValues.First();
                var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value;
                if (string.IsNullOrEmpty(boundary))
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteStringAsync("Missing boundary in Content-Type.");
                    return badResp;
                }

                var reader = new MultipartReader(boundary, req.Body);
                MultipartSection? section = await reader.ReadNextSectionAsync();
                string? uploadedFileName = null;

                while (section != null)
                {
                    var contentDisposition = section.GetContentDispositionHeader();
                    if (contentDisposition != null && contentDisposition.IsFileDisposition())
                    {
                        var originalFileName = contentDisposition.FileName.Value;
                        var uniqueFileName = $"{Guid.NewGuid()}-{originalFileName}";

                        // Copy section body to a MemoryStream so Azure can re-read it
                        using var memoryStream = new MemoryStream();
                        await section.Body.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        // Upload to "uploads"
                        await _fileShareService.UploadFileAsync(string.Empty, uniqueFileName, memoryStream);
                        uploadedFileName = uniqueFileName;
                        break;
                    }
                    section = await reader.ReadNextSectionAsync();
                }

                if (uploadedFileName == null)
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteStringAsync("No file uploaded.");
                    return badResp;
                }

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync(new
                {
                    Message = "File uploaded successfully",
                    FileName = uploadedFileName
                });
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync($"Internal server error: {ex.Message}");
                return resp;
            }
        }


        [Function("ListFiles")]
        public async Task<HttpResponseData> ListFiles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list-files")] HttpRequestData req)
        {
            try
            {
                var files = await _fileShareService.ListFilesAsync(_filePath);
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync(files); 
                return resp;
            }
            catch (Exception ex)
            {
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync($"Error: {ex.Message}");
                return resp;
            }
        }


        [Function("DownloadFile")]
        public async Task<HttpResponseData> DownloadFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "download-file")] HttpRequestData req)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var fileName = query["fileName"];

            if (string.IsNullOrEmpty(fileName))
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteStringAsync("fileName query parameter is required.");
                return badResp;
            }

            try
            {
                _logger.LogInformation($"Attempting to download '{fileName}' from root of share '{_fileShareName}'.");
                var stream = await _fileShareService.DownloadFileAsync("", fileName);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                resp.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                resp.Headers.Add("Content-Type", "application/octet-stream");

                // Copy stream directly to response
                await stream.CopyToAsync(resp.Body);
                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file '{fileName}': {ex.Message}");
                var resp = req.CreateResponse(HttpStatusCode.NotFound);
                await resp.WriteStringAsync($"File not found: {fileName}");
                return resp;
            }
        }


        [Function("DeleteFile")]
        public async Task<HttpResponseData> DeleteFile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "delete-file")] HttpRequestData req)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var fileName = query["fileName"];
            if (string.IsNullOrEmpty(fileName))
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteStringAsync("fileName query parameter is required.");
                return badResp;
            }

            await _fileShareService.DeleteFileAsync(_filePath, fileName);
            var resp = req.CreateResponse(HttpStatusCode.OK);
            await resp.WriteStringAsync($"File '{fileName}' deleted successfully.");
            return resp;
        }
    }
}