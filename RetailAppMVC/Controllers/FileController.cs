using Microsoft.AspNetCore.Mvc;
using RetailAppCore.Models;
using RetailAppCore.Services;

namespace RetailAppMVC.Controllers
{
    public class FileController : Controller
    {
        private readonly FileService _fileService;

        public FileController(FileService fileService)
        {
            _fileService = fileService;
        }

        // Index - List files
        public async Task<IActionResult> Index()
        {
            var files = await _fileService.ListAsync();
            return View(files);
        }

        // Upload file form
        public IActionResult Upload()
        {
            return View();
        }

        // Upload file
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to upload.");
                TempData["Error"] = "Please select a file to upload.";
                return View();
            }

            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            content.Add(new StreamContent(stream), "file", file.FileName);

            await _fileService.UploadAsync(content);

            TempData["Message"] = "File Uploaded Successfully";
            return RedirectToAction(nameof(Index));
        }


        // Download file        
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name is required");

            var fileBytes = await _fileService.DownloadAsync(fileName);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        // Delete file
        public async Task<IActionResult> Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name is required");

            await _fileService.DeleteAsync(fileName);
            TempData["Message"] = $"File '{fileName}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}