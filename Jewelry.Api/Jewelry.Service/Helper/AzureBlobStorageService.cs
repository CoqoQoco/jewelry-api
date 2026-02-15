using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using jewelry.Model.Azure;
using jewelry.Model.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Helper
{
    public interface IAzureBlobStorageService
    {
        /// <summary>
        /// Upload ไฟล์ไปยัง Azure Blob Storage
        /// </summary>
        /// <param name="fileStream">Stream ของไฟล์</param>
        /// <param name="folderName">ชื่อ folder เช่น "Mold", "Stock"</param>
        /// <param name="fileName">ชื่อไฟล์ เช่น "image1.jpg"</param>
        /// <returns>UploadResult พร้อม URL</returns>
        Task<UploadResult> UploadImageAsync(Stream fileStream, string folderName, string fileName);

        /// <summary>
        /// แสดงรายการไฟล์ใน folder ที่ระบุ
        /// </summary>
        Task<List<BlobFileInfo>> ListImagesByFolderAsync(string folderName);

        /// <summary>
        /// Download ไฟล์จาก Azure Blob
        /// </summary>
        Task<Stream> DownloadImageAsync(string folderName, string fileName);

        /// <summary>
        /// ลบไฟล์
        /// </summary>
        Task<bool> DeleteImageAsync(string folderName, string fileName);

        /// <summary>
        /// ดึง URL ของไฟล์
        /// </summary>
        string GetImageUrl(string folderName, string fileName);

        /// <summary>
        /// Migrate ไฟล์จาก Local Server มาที่ Azure
        /// </summary>
        Task MigrateFromLocalAsync(string localBasePath);
    }

    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly List<string> _allowedFolders;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(
            IOptions<AzureStorageConfig> config,
            ILogger<AzureBlobStorageService> logger)
        {
            var connectionString = config.Value.ConnectionString
                ?? throw new InvalidOperationException("Azure Storage connection string is not configured.");

            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerName = config.Value.ContainerName ?? "jewelry-images";
            _allowedFolders = config.Value.AllowedFolders ?? new List<string>();
            _logger = logger;
        }

        public async Task<UploadResult> UploadImageAsync(
            Stream fileStream,
            string folderName,
            string fileName)
        {
            try
            {
                // 1. Validate folder name
                if (!_allowedFolders.Contains(folderName))
                {
                    _logger.LogWarning($"Folder '{folderName}' is not in allowed list");
                    return new UploadResult
                    {
                        Success = false,
                        ErrorMessage = $"Folder '{folderName}' is not allowed"
                    };
                }

                // 2. Get container client
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

                // 3. สร้าง container ถ้ายังไม่มี (ครั้งแรกเท่านั้น)
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                // 4. สร้าง blob name ด้วย virtual folder path
                // ตัวอย่าง: "Mold/image1.jpg"
                var blobName = $"{folderName}/{fileName}";

                _logger.LogInformation($"Uploading blob: {blobName}");

                // 5. Get blob client
                var blobClient = containerClient.GetBlobClient(blobName);

                // 6. กำหนด content type
                var contentType = GetContentType(fileName);

                // 7. Upload file
                await blobClient.UploadAsync(
                    fileStream,
                    new BlobHttpHeaders { ContentType = contentType },
                    conditions: null // overwrite if exists
                );

                _logger.LogInformation($"Successfully uploaded: {blobName}");

                return new UploadResult
                {
                    Success = true,
                    Url = blobClient.Uri.ToString(),
                    BlobName = blobName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload {folderName}/{fileName}");
                return new UploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<List<BlobFileInfo>> ListImagesByFolderAsync(string folderName)
        {
            var files = new List<BlobFileInfo>();
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            // ใช้ prefix เพื่อ filter blobs ที่ขึ้นต้นด้วย "Mold/"
            // Azure จะ return เฉพาะ blobs ที่มีชื่อขึ้นต้นด้วย prefix นี้
            var prefix = $"{folderName}/";

            _logger.LogInformation($"Listing blobs with prefix: {prefix}");

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);

                files.Add(new BlobFileInfo
                {
                    FileName = Path.GetFileName(blobItem.Name), // "image1.jpg"
                    FolderName = folderName,                     // "Mold"
                    FullPath = blobItem.Name,                    // "Mold/image1.jpg"
                    Url = blobClient.Uri.ToString(),
                    Size = blobItem.Properties.ContentLength,
                    LastModified = blobItem.Properties.LastModified
                });
            }

            _logger.LogInformation($"Found {files.Count} files in {folderName}");
            return files;
        }

        public async Task<Stream> DownloadImageAsync(string folderName, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName = $"{folderName}/{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogInformation($"Downloading blob: {blobName}");

            var download = await blobClient.DownloadAsync();
            return download.Value.Content;
        }

        public async Task<bool> DeleteImageAsync(string folderName, string fileName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobName = $"{folderName}/{fileName}";
                var blobClient = containerClient.GetBlobClient(blobName);

                _logger.LogInformation($"Deleting blob: {blobName}");

                await blobClient.DeleteIfExistsAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete {folderName}/{fileName}");
                return false;
            }
        }

        public string GetImageUrl(string folderName, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName = $"{folderName}/{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);
            return blobClient.Uri.ToString();
        }

        public async Task MigrateFromLocalAsync(string localBasePath)
        {
            _logger.LogInformation($"Starting migration from: {localBasePath}");

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var totalFiles = 0;
            var successCount = 0;
            var errorCount = 0;

            foreach (var folderName in _allowedFolders)
            {
                var localFolderPath = Path.Combine(localBasePath, folderName);

                if (!Directory.Exists(localFolderPath))
                {
                    _logger.LogWarning($"Folder not found: {localFolderPath}");
                    continue;
                }

                var files = Directory.GetFiles(localFolderPath, "*.*", SearchOption.AllDirectories);
                totalFiles += files.Length;

                _logger.LogInformation($"Migrating {files.Length} files from {folderName}...");

                foreach (var filePath in files)
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        var blobName = $"{folderName}/{fileName}";

                        var blobClient = containerClient.GetBlobClient(blobName);

                        using var fileStream = File.OpenRead(filePath);
                        var contentType = GetContentType(fileName);

                        await blobClient.UploadAsync(
                            fileStream,
                            new BlobHttpHeaders { ContentType = contentType },
                            conditions: null
                        );

                        successCount++;
                        _logger.LogInformation($"✓ [{successCount}/{totalFiles}] Uploaded: {blobName}");
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, $"✗ Failed: {filePath}");
                    }
                }
            }

            _logger.LogInformation($"Migration completed: {successCount} success, {errorCount} errors out of {totalFiles} total files");
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
    }
}
