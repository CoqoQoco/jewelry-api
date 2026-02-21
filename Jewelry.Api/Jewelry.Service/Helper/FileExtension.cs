using Jewelry.Data.Context;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Helper
{
    public interface IFileExtension
    {
        Task<Stream?> GetPlanImage(string imageName);
        Task<string> GetPlanImageBase64String(string imageName);
        Task<string> GetMoldImageBase64String(string imageName);

        Task<string> GetImageBase64String(string imageName, string path);


        Task<string> GetPlanMoldDesignImageBase64String(string imageName);
        Task<string> GetPlanMoldResinImageBase64String(string imageName);

        Task<string> GetStockProductImageBase64String(string imageName);
    }
    public class FileExtension : IFileExtension
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private readonly IHostEnvironment _hostingEnvironment;
        private readonly IAzureBlobStorageService _azureBlobService;

        public FileExtension(
            JewelryContext jewelryContext,
            IHostEnvironment hostingEnvironment,
            IAzureBlobStorageService azureBlobService)
        {
            _jewelryContext = jewelryContext;
            _hostingEnvironment = hostingEnvironment;
            _azureBlobService = azureBlobService;
        }

        public async Task<Stream?> GetPlanImage(string imageName)
        {
            try
            {
                // Download from Azure Blob Storage - ProductionPlan folder
                var stream = await _azureBlobService.DownloadImageAsync("ProductionPlan", imageName);
                return stream;
            }
            catch
            {
                return null;
            }
        }
        public async Task<string> GetPlanImageBase64String(string imageName)
        {
            try
            {
                // Download from Azure Blob Storage - ProductionPlan folder
                var stream = await _azureBlobService.DownloadImageAsync("ProductionPlan", imageName);

                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    return Convert.ToBase64String(imageBytes);
                }
            }
            catch
            {
                return null;
            }
        }
        public async Task<string> GetMoldImageBase64String(string imageName)
        {
            try
            {
                // Download from Azure Blob Storage - Mold folder
                var stream = await _azureBlobService.DownloadImageAsync("Mold", imageName);

                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    return Convert.ToBase64String(imageBytes);
                }
            }
            catch
            {
                return null;
            }
        }
        public async Task<string> GetPlanMoldDesignImageBase64String(string imageName)
        {
            try
            {
                // Download from Azure Blob Storage - MoldPlanDesign folder
                var stream = await _azureBlobService.DownloadImageAsync("MoldPlanDesign", imageName);

                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    return Convert.ToBase64String(imageBytes);
                }
            }
            catch
            {
                return null;
            }
        }
        public async Task<string> GetPlanMoldResinImageBase64String(string imageName)
        {
            try
            {
                // Download from Azure Blob Storage - MoldPlanResin folder
                var stream = await _azureBlobService.DownloadImageAsync("MoldPlanResin", imageName);

                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    return Convert.ToBase64String(imageBytes);
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetStockProductImageBase64String(string imageName)
        {
            try
            {
                // Download from Azure Blob Storage - Stock folder
                var stream = await _azureBlobService.DownloadImageAsync("Stock/Product", imageName);

                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    return Convert.ToBase64String(imageBytes);
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetImageBase64String(string imageName, string path)
        {
            try
            {
                // Map old local path to Azure Blob folder name
                // Example: "Images/Mold" -> "Mold", "Images/OrderPlan" -> "ProductionPlan"
                string folderName = path
                    .Replace("Images/", "")
                    .Replace("Images\\", "")
                    .Replace("OrderPlan", "ProductionPlan")
                    .Replace("Stock/Product", "Stock")
                    .Replace("Stock\\Product", "Stock");

                // Download from Azure Blob Storage
                var stream = await _azureBlobService.DownloadImageAsync(folderName, imageName);

                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    return Convert.ToBase64String(imageBytes);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
