using RetailAppCore.Models;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace RetailAppCore.Services
{
    public class FileShareService
    {
        private readonly string _connectionString = ""; // Replace with your actual Azure File Share connection string
        private readonly string _fileShareName = ""; // Replace with your actual Azure File Share name

        // Constructor to initialize the FileShareService with connection string and file share name
        public FileShareService(string connectionString, string fileShareName)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _fileShareName = fileShareName ?? throw new ArgumentNullException(nameof(fileShareName));
        }

        // Upload a file to the specified path in the file share
        public async Task UploadFileAsync(string filePath, string fileName, Stream fileStream)
        {
            var serviceClient = new ShareServiceClient(_connectionString);
            var shareClient = serviceClient.GetShareClient(_fileShareName);
            var directoryClient = shareClient.GetDirectoryClient(filePath);
            var fileClient = directoryClient.GetFileClient(fileName);
            await fileClient.CreateAsync(fileStream.Length);
            await fileClient.UploadRangeAsync(new HttpRange(0, fileStream.Length), fileStream);
        }

        // Download a file from the specified path in the file share
        public async Task<Stream> DownloadFileAsync(string filePath, string fileName)
        {
            try
            {
                var serviceClient = new ShareServiceClient(_connectionString);
                var shareClient = serviceClient.GetShareClient(_fileShareName);

                var directoryClient = string.IsNullOrEmpty(filePath)
                    ? shareClient.GetRootDirectoryClient()  
                    : shareClient.GetDirectoryClient(filePath);

                var fileClient = directoryClient.GetFileClient(fileName);

                if (!await fileClient.ExistsAsync())
                    throw new FileNotFoundException($"File '{fileName}' not found in the file share root.");

                var downloadResponse = await fileClient.DownloadAsync();
                return downloadResponse.Value.Content;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download file '{fileName}' from Azure File Share", ex);
            }
        }


        // List files in the specified path in the file share
        public async Task<List<FileModel>> ListFilesAsync(string filePath)
        {
            var fileModels = new List<FileModel>();
            try
            {
                var serviceClient = new ShareServiceClient(_connectionString);
                var shareClient = serviceClient.GetShareClient(_fileShareName);

                var directoryClient = shareClient.GetDirectoryClient(filePath);

                await foreach (ShareFileItem item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        var fileClient = directoryClient.GetFileClient(item.Name);
                        var properties = await fileClient.GetPropertiesAsync();
                        fileModels.Add(new FileModel
                        {
                            FileName = item.Name,
                            FileSize = properties.Value.ContentLength,
                            LastModified = properties.Value.LastModified
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to list files from Azure File Share", ex);
            }
            return fileModels;
        }

        // Delete a file from the specified path in the file share
        public async Task DeleteFileAsync(string filePath, string fileName)
        {
            try
            {
                var serviceClient = new ShareServiceClient(_connectionString);
                var shareClient = serviceClient.GetShareClient(_fileShareName);
                var directoryClient = shareClient.GetDirectoryClient(filePath);
                var fileClient = directoryClient.GetFileClient(fileName);

                await fileClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete file '{fileName}' from Azure File Share", ex);
            }
        }
    }
}
