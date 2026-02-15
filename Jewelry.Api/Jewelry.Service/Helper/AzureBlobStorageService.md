# Azure Blob Storage Implementation Guide
## 1 Container + Virtual Folders Architecture

> **คู่มือนี้สำหรับ:** Claude Code เพื่อใช้อ้างอิงในการ implement Azure Blob Storage  
> **Architecture:** Single Container with Virtual Folder Structure  
> **Use Case:** Jewelry Inventory System - 5GB Images from Local Server

---

## 📖 Table of Contents

1. [Concept Overview](#concept-overview)
2. [Architecture Design](#architecture-design)
3. [Implementation Code](#implementation-code)
4. [API Examples](#api-examples)
5. [Frontend Integration](#frontend-integration)
6. [Migration Process](#migration-process)
7. [Key Points & Best Practices](#key-points--best-practices)

---

## 🎯 Concept Overview

### Azure Blob Storage ไม่มี "Folder" จริงๆ

**สิ่งสำคัญที่ต้องเข้าใจ:**
- Azure Blob Storage เก็บข้อมูลเป็น **flat list** ของ blobs (files)
- **"Folders"** เป็นแค่ **virtual structure** ที่สร้างจากชื่อของ blob ที่มี `/` อยู่ในชื่อ
- เมื่อตั้งชื่อ blob เป็น `"Mold/image1.jpg"` → Azure Portal และ Storage Explorer จะแสดงเป็น folder structure

### ตัวอย่างเปรียบเทียบ

**Local File System (ของจริง):**
```
D:\Images\
├── Mold\
│   ├── image1.jpg
│   └── image2.jpg
├── Stock\
│   └── product.jpg
└── User\
    └── avatar.jpg
```

**Azure Blob Storage (ข้อมูลจริงใน Database):**
```
Container: jewelry-images
├── Blob 1: name = "Mold/image1.jpg", data = [binary]
├── Blob 2: name = "Mold/image2.jpg", data = [binary]
├── Blob 3: name = "Stock/product.jpg", data = [binary]
└── Blob 4: name = "User/avatar.jpg", data = [binary]
```

**Azure Portal แสดง (Visual Representation):**
```
jewelry-images/
├── 📁 Mold/
│   ├── 🖼️ image1.jpg
│   └── 🖼️ image2.jpg
├── 📁 Stock/
│   └── 🖼️ product.jpg
└── 📁 User/
    └── 🖼️ avatar.jpg
```

### สิ่งที่เกิดขึ้นจริง

```csharp
// เมื่อคุณ upload ด้วยโค้ดนี้:
var blobName = "Mold/image1.jpg";  // blob name มี "/"
var blobClient = containerClient.GetBlobClient(blobName);
await blobClient.UploadAsync(fileStream);

// Azure จะเก็บ:
// - Blob Name: "Mold/image1.jpg" (เป็น string ธรรมดา)
// - Azure Portal จะ parse "/" และแสดงเป็น folder "Mold"
```

---

## 🏗️ Architecture Design

### โครงสร้างที่เราจะสร้าง

```
Azure Storage Account
└── Container: jewelry-images (1 container เดียว)
    ├── Mold/
    │   ├── mold_001.jpg
    │   ├── mold_002.jpg
    │   └── mold_003.jpg
    ├── MoldPlanCasting/
    │   ├── casting_001.jpg
    │   └── casting_002.jpg
    ├── MoldPlanCastingSilver/
    │   └── silver_001.jpg
    ├── MoldPlanCutting/
    │   └── cutting_001.jpg
    ├── MoldPlanDesign/
    │   └── design_001.jpg
    ├── MoldPlanResin/
    │   └── resin_001.jpg
    ├── OrderPlan/
    │   └── order_001.jpg
    ├── Stock/
    │   ├── stock_001.jpg
    │   └── stock_002.jpg
    └── User/
        └── avatar_001.jpg
```

### URL Pattern ที่ได้

```
https://{storage-account}.blob.core.windows.net/{container}/{blob-name}

ตัวอย่าง:
https://jewelrystore.blob.core.windows.net/jewelry-images/Mold/image1.jpg
https://jewelrystore.blob.core.windows.net/jewelry-images/Stock/product.jpg
```

### เหตุผลที่เลือก 1 Container

✅ **ข้อดี:**
- จัดการง่าย มี container เดียว
- Permission ตั้งที่ container level
- Cost-effective (operations charges ต่ำกว่า)
- Migration ง่าย (copy ทั้งหมดพร้อมกัน)
- Flexible - เพิ่ม/ลด folder ได้ง่าย

❌ **ข้อเสีย:**
- Permission ละเอียดต้องใช้ SAS Token
- ถ้า folder มากเกิน 50+ folders อาจจัดการยาก

---

## 💻 Implementation Code

### 1. Project Setup

```bash
# Create new .NET Web API project
dotnet new webapi -n JewelryImageService
cd JewelryImageService

# Install Azure Blob Storage SDK
dotnet add package Azure.Storage.Blobs
```

### 2. Models

```csharp
// Models/BlobFileInfo.cs
namespace JewelryImageService.Models;

/// <summary>
/// ข้อมูลของไฟล์ใน Blob Storage
/// </summary>
public class BlobFileInfo
{
    /// <summary>
    /// ชื่อไฟล์เท่านั้น เช่น "image1.jpg"
    /// </summary>
    public string FileName { get; set; }
    
    /// <summary>
    /// ชื่อ folder เช่น "Mold"
    /// </summary>
    public string FolderName { get; set; }
    
    /// <summary>
    /// Path เต็ม เช่น "Mold/image1.jpg"
    /// </summary>
    public string FullPath { get; set; }
    
    /// <summary>
    /// URL เต็มสำหรับเข้าถึงไฟล์
    /// </summary>
    public string Url { get; set; }
    
    /// <summary>
    /// ขนาดไฟล์ (bytes)
    /// </summary>
    public long? Size { get; set; }
    
    /// <summary>
    /// วันที่แก้ไขล่าสุด
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }
}
```

```csharp
// Models/UploadResult.cs
namespace JewelryImageService.Models;

/// <summary>
/// ผลลัพธ์การ Upload
/// </summary>
public class UploadResult
{
    public bool Success { get; set; }
    public string Url { get; set; }
    public string BlobName { get; set; }
    public string ErrorMessage { get; set; }
}
```

```csharp
// Models/AzureStorageConfig.cs
namespace JewelryImageService.Models;

/// <summary>
/// Configuration สำหรับ Azure Storage
/// </summary>
public class AzureStorageConfig
{
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; }
    public List<string> AllowedFolders { get; set; }
}
```

### 3. Configuration

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=youraccountkey;EndpointSuffix=core.windows.net",
    "ContainerName": "jewelry-images",
    "AllowedFolders": [
      "Mold",
      "MoldPlanCasting",
      "MoldPlanCastingSilver",
      "MoldPlanCutting",
      "MoldPlanDesign",
      "MoldPlanResin",
      "OrderPlan",
      "Stock",
      "User"
    ]
  }
}
```

### 4. Core Service Interface

```csharp
// Services/IBlobStorageService.cs
namespace JewelryImageService.Services;

public interface IBlobStorageService
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
```

### 5. Core Service Implementation

```csharp
// Services/BlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using JewelryImageService.Models;
using Microsoft.Extensions.Options;

namespace JewelryImageService.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly List<string> _allowedFolders;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        IOptions<AzureStorageConfig> config,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = new BlobServiceClient(config.Value.ConnectionString);
        _containerName = config.Value.ContainerName;
        _allowedFolders = config.Value.AllowedFolders;
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
```

### 6. Dependency Injection Registration

```csharp
// Program.cs
using JewelryImageService.Models;
using JewelryImageService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Azure Storage
builder.Services.Configure<AzureStorageConfig>(
    builder.Configuration.GetSection("AzureStorage")
);

// Register BlobStorageService as Singleton
// (Singleton เพราะ BlobServiceClient เป็น thread-safe และ reusable)
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vue dev server
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowVueApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 🌐 API Examples

### Controller Implementation

```csharp
// Controllers/ImageController.cs
using JewelryImageService.Models;
using JewelryImageService.Services;
using Microsoft.AspNetCore.Mvc;

namespace JewelryImageService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IBlobStorageService _blobService;
    private readonly ILogger<ImageController> _logger;

    public ImageController(
        IBlobStorageService blobService,
        ILogger<ImageController> logger)
    {
        _blobService = blobService;
        _logger = logger;
    }

    /// <summary>
    /// Upload single image
    /// </summary>
    /// <param name="file">Image file</param>
    /// <param name="folderName">Folder name (Mold, Stock, etc.)</param>
    /// <returns>Upload result with URL</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string folderName)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (string.IsNullOrEmpty(folderName))
            return BadRequest(new { error = "Folder name is required" });

        using var stream = file.OpenReadStream();
        var result = await _blobService.UploadImageAsync(
            stream, 
            folderName, 
            file.FileName
        );

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new
        {
            success = true,
            url = result.Url,
            blobName = result.BlobName,
            fileName = file.FileName,
            folder = folderName
        });
    }

    /// <summary>
    /// Upload multiple images
    /// </summary>
    [HttpPost("upload/multiple")]
    public async Task<IActionResult> UploadMultiple(
        [FromForm] List<IFormFile> files,
        [FromForm] string folderName)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "No files uploaded" });

        var results = new List<object>();

        foreach (var file in files)
        {
            using var stream = file.OpenReadStream();
            var result = await _blobService.UploadImageAsync(
                stream,
                folderName,
                file.FileName
            );

            results.Add(new
            {
                fileName = file.FileName,
                success = result.Success,
                url = result.Url,
                error = result.ErrorMessage
            });
        }

        return Ok(new
        {
            totalFiles = files.Count,
            results
        });
    }

    /// <summary>
    /// List all images in a folder
    /// </summary>
    /// <param name="folderName">Folder name</param>
    [HttpGet("list/{folderName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListImages(string folderName)
    {
        var images = await _blobService.ListImagesByFolderAsync(folderName);
        
        return Ok(new
        {
            folder = folderName,
            count = images.Count,
            images
        });
    }

    /// <summary>
    /// Get image URL
    /// </summary>
    [HttpGet("url/{folderName}/{fileName}")]
    public IActionResult GetImageUrl(string folderName, string fileName)
    {
        var url = _blobService.GetImageUrl(folderName, fileName);
        return Ok(new { url, folderName, fileName });
    }

    /// <summary>
    /// Download image
    /// </summary>
    [HttpGet("download/{folderName}/{fileName}")]
    public async Task<IActionResult> DownloadImage(string folderName, string fileName)
    {
        try
        {
            var stream = await _blobService.DownloadImageAsync(folderName, fileName);
            var contentType = GetContentType(fileName);
            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to download {folderName}/{fileName}");
            return NotFound(new { error = "File not found" });
        }
    }

    /// <summary>
    /// Delete image
    /// </summary>
    [HttpDelete("{folderName}/{fileName}")]
    public async Task<IActionResult> DeleteImage(string folderName, string fileName)
    {
        var success = await _blobService.DeleteImageAsync(folderName, fileName);
        
        if (!success)
            return NotFound(new { error = "File not found or already deleted" });

        return Ok(new { 
            success = true, 
            message = $"Deleted {folderName}/{fileName}" 
        });
    }

    /// <summary>
    /// Migrate from local server (Development only)
    /// </summary>
    [HttpPost("migrate")]
    public async Task<IActionResult> Migrate([FromBody] MigrateRequest request)
    {
        if (string.IsNullOrEmpty(request.LocalPath))
            return BadRequest(new { error = "LocalPath is required" });

        if (!Directory.Exists(request.LocalPath))
            return BadRequest(new { error = "LocalPath does not exist" });

        await _blobService.MigrateFromLocalAsync(request.LocalPath);
        
        return Ok(new { 
            success = true, 
            message = "Migration completed. Check logs for details." 
        });
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
            _ => "application/octet-stream"
        };
    }
}

public class MigrateRequest
{
    public string LocalPath { get; set; }
}
```

---

## 🎨 Frontend Integration (Vue.js)

### Example Vue Component

```vue
<!-- components/ImageUpload.vue -->
<template>
  <div class="image-upload-container">
    <h2>Upload Images</h2>
    
    <!-- Folder Selection -->
    <div class="form-group">
      <label>Select Folder:</label>
      <select v-model="selectedFolder" class="form-control">
        <option value="">-- เลือก Folder --</option>
        <option value="Mold">Mold</option>
        <option value="MoldPlanCasting">MoldPlanCasting</option>
        <option value="MoldPlanCastingSilver">MoldPlanCastingSilver</option>
        <option value="MoldPlanCutting">MoldPlanCutting</option>
        <option value="MoldPlanDesign">MoldPlanDesign</option>
        <option value="MoldPlanResin">MoldPlanResin</option>
        <option value="OrderPlan">OrderPlan</option>
        <option value="Stock">Stock</option>
        <option value="User">User</option>
      </select>
    </div>

    <!-- File Input -->
    <div class="form-group">
      <label>Select Images:</label>
      <input 
        type="file" 
        @change="onFileChange" 
        accept="image/*"
        multiple
        class="form-control"
      />
      <small v-if="files.length > 0">
        Selected: {{ files.length }} file(s)
      </small>
    </div>

    <!-- Upload Button -->
    <button 
      @click="uploadImages" 
      :disabled="!canUpload || uploading"
      class="btn btn-primary"
    >
      <span v-if="!uploading">Upload Images</span>
      <span v-else>Uploading... {{ uploadProgress }}%</span>
    </button>

    <!-- Upload Results -->
    <div v-if="uploadResults.length > 0" class="results">
      <h3>Upload Results:</h3>
      <div 
        v-for="(result, index) in uploadResults" 
        :key="index"
        :class="['result-item', result.success ? 'success' : 'error']"
      >
        <div class="result-info">
          <strong>{{ result.fileName }}</strong>
          <span v-if="result.success">✓ Success</span>
          <span v-else>✗ {{ result.error }}</span>
        </div>
        <img 
          v-if="result.success && result.url" 
          :src="result.url" 
          class="thumbnail"
          :alt="result.fileName"
        />
      </div>
    </div>

    <!-- Image Gallery -->
    <div v-if="selectedFolder" class="gallery">
      <h3>Images in {{ selectedFolder }}</h3>
      <button @click="loadImages" class="btn btn-secondary">
        Refresh Gallery
      </button>
      
      <div v-if="loading" class="loading">Loading...</div>
      
      <div v-else class="image-grid">
        <div 
          v-for="image in images" 
          :key="image.fullPath"
          class="image-card"
        >
          <img :src="image.url" :alt="image.fileName" />
          <div class="image-info">
            <p>{{ image.fileName }}</p>
            <small>{{ formatSize(image.size) }}</small>
            <button @click="deleteImage(image)" class="btn-delete">
              Delete
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import axios from 'axios';

const API_BASE = 'http://localhost:5000/api/image';

// State
const selectedFolder = ref('');
const files = ref([]);
const uploading = ref(false);
const uploadProgress = ref(0);
const uploadResults = ref([]);
const images = ref([]);
const loading = ref(false);

// Computed
const canUpload = computed(() => {
  return selectedFolder.value && files.value.length > 0;
});

// Watch folder change
watch(selectedFolder, (newFolder) => {
  if (newFolder) {
    loadImages();
  }
});

// Methods
const onFileChange = (event) => {
  files.value = Array.from(event.target.files);
  uploadResults.value = [];
};

const uploadImages = async () => {
  if (!canUpload.value) return;

  uploading.value = true;
  uploadResults.value = [];
  uploadProgress.value = 0;

  const totalFiles = files.value.length;

  for (let i = 0; i < files.value.length; i++) {
    const file = files.value[i];
    const formData = new FormData();
    formData.append('file', file);
    formData.append('folderName', selectedFolder.value);

    try {
      const response = await axios.post(`${API_BASE}/upload`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      
      uploadResults.value.push({
        fileName: file.name,
        success: true,
        url: response.data.url
      });
    } catch (error) {
      uploadResults.value.push({
        fileName: file.name,
        success: false,
        error: error.response?.data?.error || 'Upload failed'
      });
    }

    uploadProgress.value = Math.round(((i + 1) / totalFiles) * 100);
  }

  uploading.value = false;
  files.value = [];
  
  // Reload gallery
  await loadImages();
};

const loadImages = async () => {
  if (!selectedFolder.value) return;

  loading.value = true;
  try {
    const response = await axios.get(`${API_BASE}/list/${selectedFolder.value}`);
    images.value = response.data.images;
  } catch (error) {
    console.error('Failed to load images:', error);
    images.value = [];
  } finally {
    loading.value = false;
  }
};

const deleteImage = async (image) => {
  if (!confirm(`Delete ${image.fileName}?`)) return;

  try {
    await axios.delete(`${API_BASE}/${image.folderName}/${image.fileName}`);
    await loadImages(); // Reload
  } catch (error) {
    alert('Failed to delete image');
  }
};

const formatSize = (bytes) => {
  if (!bytes) return 'N/A';
  const mb = bytes / (1024 * 1024);
  return mb < 1 
    ? `${(bytes / 1024).toFixed(1)} KB`
    : `${mb.toFixed(2)} MB`;
};
</script>

<style scoped>
.image-upload-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

.form-group {
  margin-bottom: 15px;
}

.form-control {
  width: 100%;
  padding: 8px;
  margin-top: 5px;
}

.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}

.btn-primary {
  background-color: #007bff;
  color: white;
}

.btn-primary:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}

.results {
  margin-top: 20px;
}

.result-item {
  padding: 10px;
  margin: 10px 0;
  border-radius: 4px;
  display: flex;
  gap: 15px;
  align-items: center;
}

.result-item.success {
  background-color: #d4edda;
  border: 1px solid #c3e6cb;
}

.result-item.error {
  background-color: #f8d7da;
  border: 1px solid #f5c6cb;
}

.thumbnail {
  width: 100px;
  height: 100px;
  object-fit: cover;
  border-radius: 4px;
}

.gallery {
  margin-top: 40px;
}

.image-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 15px;
  margin-top: 20px;
}

.image-card {
  border: 1px solid #ddd;
  border-radius: 8px;
  overflow: hidden;
}

.image-card img {
  width: 100%;
  height: 200px;
  object-fit: cover;
}

.image-info {
  padding: 10px;
}

.btn-delete {
  background-color: #dc3545;
  color: white;
  padding: 5px 10px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  margin-top: 5px;
}

.loading {
  text-align: center;
  padding: 20px;
}
</style>
```

---

## 🔄 Migration Process

### Method 1: Using Migration API

```bash
# Call migration endpoint
POST http://localhost:5000/api/image/migrate
Content-Type: application/json

{
  "localPath": "D:\\LocalServerImages"
}
```

### Method 2: Using AzCopy (Recommended for 5GB)

```bash
# 1. Download AzCopy
# https://aka.ms/downloadazcopy-v10

# 2. Get SAS Token from Azure Portal
# Storage Account > Shared access signature
# - Allowed services: Blob
# - Allowed resource types: Container, Object
# - Allowed permissions: Read, Write, Create
# - Generate SAS token

# 3. Upload with folder structure
azcopy copy "D:\LocalServerImages\*" ^
  "https://youraccountname.blob.core.windows.net/jewelry-images?YOUR_SAS_TOKEN" ^
  --recursive=true

# Windows PowerShell:
azcopy copy "D:\LocalServerImages\*" `
  "https://youraccountname.blob.core.windows.net/jewelry-images?YOUR_SAS_TOKEN" `
  --recursive=true
```

### Method 3: Using Azure Storage Explorer (GUI)

1. Download: https://azure.microsoft.com/features/storage-explorer/
2. Sign in with Azure account
3. Navigate to Storage Account > Containers > jewelry-images
4. Click "Upload" > "Upload Folder"
5. Select local folder and upload

---

## 🎯 Key Points & Best Practices

### 1. Blob Naming Convention

```csharp
// ✅ CORRECT: Virtual folder structure
var blobName = "Mold/image1.jpg";        // Creates virtual "Mold" folder
var blobName = "Stock/product_123.jpg";  // Creates virtual "Stock" folder

// ❌ WRONG: No folder structure
var blobName = "image1.jpg";             // Flat structure
var blobName = "Mold-image1.jpg";        // Not a folder
```

### 2. Prefix Filtering

```csharp
// List all blobs in "Mold" folder
var prefix = "Mold/";
await foreach (var blob in containerClient.GetBlobsAsync(prefix: prefix))
{
    // Only gets blobs starting with "Mold/"
    Console.WriteLine(blob.Name);
}

// Output:
// Mold/image1.jpg
// Mold/image2.jpg
// (NOT: Stock/product.jpg)
```

### 3. URL Structure

```
Pattern: https://{account}.blob.core.windows.net/{container}/{blob-name}

Example:
https://jewelrystore.blob.core.windows.net/jewelry-images/Mold/image1.jpg
         └──account──┘                       └─container─┘ └─blob-name─┘
```

### 4. Content Type Best Practices

```csharp
// Always set correct Content-Type for proper browser display
var headers = new BlobHttpHeaders 
{ 
    ContentType = "image/jpeg",           // Browser shows as image
    CacheControl = "public, max-age=31536000"  // Cache for 1 year
};

await blobClient.UploadAsync(stream, headers);
```

### 5. Security Best Practices

```csharp
// ❌ DON'T: Store Connection String in code
var connectionString = "DefaultEndpointsProtocol=https;AccountName=...";

// ✅ DO: Use Configuration
var connectionString = configuration["AzureStorage:ConnectionString"];

// ✅ BETTER: Use Azure Key Vault in production
// ✅ BEST: Use Managed Identity (no secrets)
```

### 6. Error Handling

```csharp
try
{
    await blobClient.UploadAsync(stream);
}
catch (Azure.RequestFailedException ex) when (ex.Status == 404)
{
    // Container not found
}
catch (Azure.RequestFailedException ex) when (ex.Status == 409)
{
    // Blob already exists
}
catch (Exception ex)
{
    // General error
    _logger.LogError(ex, "Upload failed");
}
```

### 7. Performance Tips

```csharp
// ✅ Reuse BlobServiceClient (thread-safe)
services.AddSingleton<BlobServiceClient>();

// ✅ Upload in parallel for multiple files
var uploadTasks = files.Select(async file =>
{
    await blobClient.UploadAsync(file);
});
await Task.WhenAll(uploadTasks);

// ✅ Use appropriate block size for large files
var uploadOptions = new BlobUploadOptions
{
    TransferOptions = new StorageTransferOptions
    {
        MaximumConcurrency = 4,
        MaximumTransferLength = 4 * 1024 * 1024 // 4MB blocks
    }
};
```

### 8. Cost Optimization

```csharp
// Set access tier for infrequently accessed images
await blobClient.SetAccessTierAsync(AccessTier.Cool);

// Lifecycle management (via Azure Portal)
// - Move to Cool tier after 30 days
// - Move to Archive tier after 90 days
// - Delete after 365 days
```

---

## 📊 Comparison Table

| Aspect | 1 Container (Virtual Folders) | Multiple Containers (9 Containers) |
|--------|------------------------------|-----------------------------------|
| **Setup** | ✅ Easy - สร้าง 1 container | ❌ Complex - สร้าง 9 containers |
| **Migration** | ✅ Simple - upload ทีเดียว | ❌ Complex - upload แยก 9 ครั้ง |
| **Cost** | ✅ Lower - operations ต่ำ | ❌ Higher - operations มากกว่า |
| **Permission** | ⚠️ Container-level only | ✅ Fine-grained per container |
| **Management** | ✅ Simple - จัดการที่เดียว | ❌ Complex - จัดการ 9 ที่ |
| **Flexibility** | ✅ Easy to add folders | ❌ Need to create new container |
| **URL** | `../jewelry-images/Mold/..` | `../mold/..` |

---

## 🔍 Troubleshooting

### Problem 1: Container not found (404)

```csharp
// Solution: Create container if not exists
await containerClient.CreateIfNotExistsAsync();
```

### Problem 2: Access denied (403)

```
Check:
1. Connection String ถูกต้องหรือไม่
2. SAS Token หมดอายุหรือไม่
3. Permission ถูกต้องหรือไม่
```

### Problem 3: Image ไม่แสดงใน browser

```csharp
// Solution: Set correct Content-Type
var headers = new BlobHttpHeaders 
{ 
    ContentType = "image/jpeg" // Not "application/octet-stream"
};
```

### Problem 4: Upload ช้า

```
Solutions:
1. ใช้ AzCopy แทนการ upload ทาง API
2. เพิ่ม MaximumConcurrency
3. ใช้ Azure CDN ถ้ามี traffic สูง
```

---

## 📚 Additional Resources

- Azure Blob Storage Docs: https://docs.microsoft.com/azure/storage/blobs/
- Azure Storage SDK for .NET: https://docs.microsoft.com/dotnet/api/azure.storage.blobs
- AzCopy: https://docs.microsoft.com/azure/storage/common/storage-use-azcopy-v10
- Azure Storage Explorer: https://azure.microsoft.com/features/storage-explorer/

---

## ✅ Checklist for Claude Code

- [ ] สร้าง Azure Storage Account แล้ว
- [ ] สร้าง Container `jewelry-images` แล้ว
- [ ] กำหนด Public Access Level (Blob or Private)
- [ ] Copy Connection String ใส่ใน appsettings.json
- [ ] Install NuGet Package: `Azure.Storage.Blobs`
- [ ] สร้าง Models: `BlobFileInfo`, `UploadResult`, `AzureStorageConfig`
- [ ] Implement `IBlobStorageService` และ `BlobStorageService`
- [ ] Register service ใน Program.cs (Singleton)
- [ ] สร้าง ImageController พร้อม endpoints
- [ ] Test upload API ด้วย Postman/Swagger
- [ ] Test list API
- [ ] Integrate กับ Vue.js frontend
- [ ] Run migration จาก Local Server
- [ ] Verify ไฟล์ใน Azure Portal

---

**Created for:** Claude Code Re-check  
**Architecture:** Single Container + Virtual Folders  
**Date:** February 2026ว