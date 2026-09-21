using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jewelry.Service.Certificate
{
    public class CertificateService : BaseService, ICertificateService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IRunningNumber _runningNumberService;
        private readonly IAzureBlobStorageService _azureBlobService;

        private static readonly string[] AllowedKinds = { "photo", "logo" };
        private static readonly string[] AllowedBrandModes = { "dk", "customer" };

        // WALKIN ใช้ร่วมกันโดยลูกค้าหน้าร้าน 17 คน/20 invoice บน prod — ห้ามผูก brand default กับ code นี้
        // ไม่งั้นลูกค้า walk-in ทุกคนจะได้ brand เดียวกันไปด้วย
        private const string WalkInCustomerCode = "WALKIN";

        public CertificateService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
            IRunningNumber runningNumberService,
            IAzureBlobStorageService azureBlobService) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _runningNumberService = runningNumberService;
            _azureBlobService = azureBlobService;
        }

        public async Task<jewelry.Model.Certificate.UploadImage.Response> UploadImage(jewelry.Model.Certificate.UploadImage.Request request)
        {
            if (request.Image == null)
            {
                throw new HandleException("กรุณาเลือกไฟล์รูปภาพ");
            }

            if (string.IsNullOrEmpty(request.Kind) || !AllowedKinds.Contains(request.Kind))
            {
                throw new HandleException("ประเภทรูปภาพไม่ถูกต้อง");
            }

            if (string.IsNullOrEmpty(request.Image.ContentType) || !request.Image.ContentType.StartsWith("image/"))
            {
                throw new HandleException("ไฟล์ต้องเป็นรูปภาพเท่านั้น");
            }

            if (request.Image.Length > 5 * 1024 * 1024)
            {
                throw new HandleException("ขนาดรูปภาพต้องไม่เกิน 5 MB");
            }

            var ext = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
            var fileName = $"{request.Kind}-{Guid.NewGuid():N}{ext}";

            using var stream = request.Image.OpenReadStream();
            var result = await _azureBlobService.UploadImageAsync(stream, "Certificate", fileName);

            if (!result.Success)
            {
                throw new HandleException(result.ErrorMessage ?? "อัปโหลดรูปภาพไม่สำเร็จ");
            }

            return new jewelry.Model.Certificate.UploadImage.Response
            {
                Path = result.BlobName
            };
        }

        public async Task<jewelry.Model.Certificate.CustomerBrand.Response> GetCustomerBrand(string? customerCode)
        {
            var response = new jewelry.Model.Certificate.CustomerBrand.Response
            {
                CustomerCode = customerCode ?? string.Empty
            };

            if (string.IsNullOrEmpty(customerCode))
            {
                return response;
            }

            if (string.Equals(customerCode.Trim(), WalkInCustomerCode, StringComparison.OrdinalIgnoreCase))
            {
                return response;
            }

            var customer = await _jewelryContext.TbmCustomer
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == customerCode);

            if (customer != null)
            {
                response.BrandName = customer.CertBrandName;
                response.LogoPath = customer.CertLogoPath;
            }

            return response;
        }

        public async Task<jewelry.Model.Certificate.Create.Response> Create(jewelry.Model.Certificate.Create.Request request)
        {
            if (string.IsNullOrEmpty(request.InvoiceNumber))
            {
                throw new HandleException("Invoice Number is Required.");
            }

            var invoiceHeader = await _jewelryContext.TbtSaleInvoiceHeader
                .FirstOrDefaultAsync(x => x.Running == request.InvoiceNumber && x.IsDelete == false);

            if (invoiceHeader == null)
            {
                throw new HandleException($"Invoice not found: {request.InvoiceNumber}");
            }

            if (request.Certificates == null || !request.Certificates.Any())
            {
                throw new HandleException("Certificates are required.");
            }

            foreach (var item in request.Certificates)
            {
                if (string.IsNullOrEmpty(item.CertificateNo))
                {
                    throw new HandleException("Certificate No is Required.");
                }

                if (string.IsNullOrEmpty(item.StockNumber))
                {
                    throw new HandleException("Stock Number is Required.");
                }

                if (string.IsNullOrEmpty(item.Data))
                {
                    throw new HandleException("Certificate Data is Required.");
                }

                try
                {
                    using var _ = JsonDocument.Parse(item.Data);
                }
                catch (JsonException)
                {
                    throw new HandleException($"Certificate Data ของ {item.CertificateNo} ไม่ใช่ JSON ที่ถูกต้อง");
                }
            }

            if (request.Brand == null || string.IsNullOrEmpty(request.Brand.Mode) || !AllowedBrandModes.Contains(request.Brand.Mode))
            {
                throw new HandleException("Brand Mode ต้องเป็น 'dk' หรือ 'customer'");
            }

            if (request.Brand.Mode == "customer" && string.IsNullOrEmpty(request.Brand.Name))
            {
                throw new HandleException("กรุณาระบุชื่อแบรนด์ลูกค้า");
            }

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                var batch = await _runningNumberService.GenerateRunningNumberForGold("CERTBATCH");

                var certificateNos = request.Certificates.Select(c => c.CertificateNo).Distinct().ToList();
                var maxIssueNoMap = await _jewelryContext.TbtSaleCertificate
                    .Where(x => certificateNos.Contains(x.CertificateNo))
                    .GroupBy(x => x.CertificateNo)
                    .Select(g => new { CertificateNo = g.Key, MaxIssueNo = g.Max(x => x.IssueNo) })
                    .ToDictionaryAsync(x => x.CertificateNo, x => x.MaxIssueNo);

                var issuedInBatch = new Dictionary<string, int>();
                var response = new jewelry.Model.Certificate.Create.Response { Batch = batch };
                var entities = new List<TbtSaleCertificate>();

                foreach (var item in request.Certificates)
                {
                    var baseIssueNo = maxIssueNoMap.TryGetValue(item.CertificateNo, out var maxNo) ? maxNo : 0;
                    var alreadyInBatch = issuedInBatch.TryGetValue(item.CertificateNo, out var count) ? count : 0;
                    var issueNo = baseIssueNo + alreadyInBatch + 1;
                    issuedInBatch[item.CertificateNo] = alreadyInBatch + 1;

                    var running = await _runningNumberService.GenerateRunningNumberForGold("CERT");

                    entities.Add(new TbtSaleCertificate
                    {
                        Running = running,
                        Batch = batch,
                        CertificateNo = item.CertificateNo,
                        IssueNo = issueNo,
                        InvoiceRunning = invoiceHeader.Running,
                        InvoiceNo = request.InvoiceNumber,
                        StockNumber = item.StockNumber,
                        ItemNo = item.ItemNo,
                        CustomerCode = request.CustomerCode,
                        BrandMode = request.Brand.Mode,
                        BrandName = request.Brand.Name,
                        BrandLogoPath = request.Brand.LogoPath,
                        ImagePath = item.ImagePath,
                        SignerTitle = request.SignerTitle,
                        Data = item.Data,
                        CreateBy = CurrentUsername,
                        CreateDate = DateTime.UtcNow
                    });

                    response.Certificates.Add(new jewelry.Model.Certificate.Create.CertificateResult
                    {
                        Running = running,
                        CertificateNo = item.CertificateNo,
                        IssueNo = issueNo
                    });
                }

                _jewelryContext.TbtSaleCertificate.AddRange(entities);

                if (request.SaveBrandAsCustomerDefault && request.Brand.Mode == "customer" && !string.IsNullOrEmpty(request.CustomerCode)
                    && !string.Equals(request.CustomerCode.Trim(), WalkInCustomerCode, StringComparison.OrdinalIgnoreCase))
                {
                    var customer = await _jewelryContext.TbmCustomer
                        .FirstOrDefaultAsync(x => x.Code == request.CustomerCode);

                    if (customer != null)
                    {
                        customer.CertBrandName = request.Brand.Name;
                        customer.CertLogoPath = request.Brand.LogoPath;
                        customer.UpdateBy = CurrentUsername;
                        customer.UpdateDate = DateTime.UtcNow;

                        _jewelryContext.TbmCustomer.Update(customer);
                    }
                }

                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return response;
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"เกิดข้อผิดพลาดในการออกใบรับรองสินค้า: {ex.Message}");
            }
        }

        public async Task<jewelry.Model.Certificate.List.Response> List(jewelry.Model.Certificate.List.Request request)
        {
            var query = _jewelryContext.TbtSaleCertificate.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(request.InvoiceNumber))
            {
                query = query.Where(x => x.InvoiceNo == request.InvoiceNumber);
            }

            if (!string.IsNullOrEmpty(request.CertificateNo))
            {
                query = query.Where(x => x.CertificateNo == request.CertificateNo);
            }

            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                var normalized = StockNumberSearch.Normalize(request.StockNumber);
                if (!string.IsNullOrEmpty(normalized))
                {
                    query = query.Where(x => x.StockNumber.Replace("-", "") == normalized);
                }
            }

            if (!string.IsNullOrEmpty(request.CustomerCode))
            {
                query = query.Where(x => x.CustomerCode == request.CustomerCode);
            }

            query = query.OrderByDescending(x => x.CreateDate);

            var total = await query.CountAsync();

            if (request.Take.HasValue && request.Take.Value > 0)
            {
                var skip = request.Skip.HasValue && request.Skip.Value > 0 ? request.Skip.Value : 0;
                query = query.Skip(skip).Take(request.Take.Value);
            }

            var items = await query
                .Select(x => new jewelry.Model.Certificate.List.Item
                {
                    Running = x.Running,
                    Batch = x.Batch,
                    CertificateNo = x.CertificateNo,
                    IssueNo = x.IssueNo,
                    InvoiceNumber = x.InvoiceNo,
                    StockNumber = x.StockNumber,
                    ItemNo = x.ItemNo,
                    CustomerCode = x.CustomerCode,
                    BrandMode = x.BrandMode,
                    BrandName = x.BrandName,
                    BrandLogoPath = x.BrandLogoPath,
                    ImagePath = x.ImagePath,
                    SignerTitle = x.SignerTitle,
                    Data = x.Data,
                    CreateBy = x.CreateBy,
                    CreateDate = x.CreateDate
                })
                .ToListAsync();

            return new jewelry.Model.Certificate.List.Response
            {
                Data = items,
                Total = total
            };
        }
    }
}
