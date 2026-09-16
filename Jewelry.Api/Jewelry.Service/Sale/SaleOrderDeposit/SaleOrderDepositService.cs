using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.Stock;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.SaleOrderDeposit
{
    public class SaleOrderDepositService : BaseService, ISaleOrderDepositService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IRunningNumber _runningNumberService;
        private readonly IAzureBlobStorageService _azureBlobService;

        public SaleOrderDepositService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
            IRunningNumber runningNumberService,
            IAzureBlobStorageService azureBlobService) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _runningNumberService = runningNumberService;
            _azureBlobService = azureBlobService;
        }

        public async Task<string> Create(jewelry.Model.Sale.SaleOrderDeposit.Create.Request request)
        {
            if (string.IsNullOrEmpty(request.SoNumber))
            {
                throw new HandleException("Sale Order Number is Required.");
            }

            if (request.Amount <= 0)
            {
                throw new HandleException("Deposit Amount must be greater than 0.");
            }

            if (string.IsNullOrEmpty(request.PaymentName))
            {
                throw new HandleException("Payment Method is Required.");
            }

            // Validate bank fields based on payment method
            if (request.Payment == 2) // Transfer
            {
                if (string.IsNullOrEmpty(request.BankCode))
                {
                    throw new HandleException("Bank is required for Transfer payment.");
                }
            }
            else if (request.Payment == 3) // Cheque
            {
                if (string.IsNullOrEmpty(request.BankCode))
                {
                    throw new HandleException("Bank is required for Cheque payment.");
                }
                if (string.IsNullOrEmpty(request.BankBranch))
                {
                    throw new HandleException("Bank Branch is required for Cheque payment.");
                }
            }

            // Validate BankCode exists in master
            if (!string.IsNullOrEmpty(request.BankCode))
            {
                var bankExists = await _jewelryContext.TbmBank
                    .AnyAsync(x => x.Code == request.BankCode && x.IsActive == true);
                if (!bankExists)
                {
                    throw new HandleException($"Bank code '{request.BankCode}' is invalid.");
                }
            }

            var saleOrder = await _jewelryContext.TbtSaleOrder
                .FirstOrDefaultAsync(x => x.SoNumber == request.SoNumber);

            if (saleOrder == null)
            {
                throw new HandleException($"Sale Order {request.SoNumber} not found.");
            }

            if (saleOrder.Status <= 0)
            {
                throw new HandleException($"ใบสั่งขาย {request.SoNumber} ไม่ได้อยู่ในสถานะที่รับมัดจำได้");
            }

            // Generate running number ก่อนเสมอ เพราะ Next() ภายใน save context เอง
            var depositRunning = await _runningNumberService.GenerateRunningNumberForGold("DEP");

            // Handle image upload if provided
            string imagePath = string.Empty;
            if (request.ReceiptImage != null)
            {
                try
                {
                    string fileExtension = Path.GetExtension(request.ReceiptImage.FileName);
                    string fileName = $"{depositRunning}{fileExtension}";

                    using var stream = request.ReceiptImage.OpenReadStream();
                    var result = await _azureBlobService.UploadImageAsync(
                        stream,
                        "Deposit",
                        fileName
                    );

                    if (!result.Success)
                    {
                        throw new HandleException($"Failed to save receipt image: {result.ErrorMessage}");
                    }

                    imagePath = result.BlobName;
                }
                catch (Exception ex)
                {
                    throw new HandleException($"Failed to save receipt image: {ex.Message}");
                }
            }

            var deposit = new TbtSaleOrderDeposit
            {
                Running = depositRunning,
                SoNumber = request.SoNumber,
                DepositDate = request.DepositDate.UtcDateTime,

                Amount = request.Amount,
                CurrencyUnit = saleOrder.CurrencyUnit,
                CurrencyRate = saleOrder.CurrencyRate,

                Payment = request.Payment,
                PaymentName = request.PaymentName,

                BankCode = request.BankCode,
                BankBranch = request.BankBranch,
                ReferenceNumber = request.ReferenceNumber,
                ImagePath = imagePath,
                Remark = request.Remark,

                IsDelete = false,

                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow
            };

            _jewelryContext.TbtSaleOrderDeposit.Add(deposit);
            await _jewelryContext.SaveChangesAsync();

            return depositRunning;
        }

        public async Task<jewelry.Model.Sale.SaleOrderDeposit.List.Response> List(jewelry.Model.Sale.SaleOrderDeposit.List.Request request)
        {
            if (string.IsNullOrEmpty(request.SoNumber))
            {
                throw new HandleException("Sale Order Number is Required.");
            }

            var deposits = await _jewelryContext.TbtSaleOrderDeposit
                .Where(x => x.SoNumber == request.SoNumber)
                .OrderBy(x => x.DepositDate)
                .ThenBy(x => x.CreateDate)
                .ToListAsync();

            var depositRunnings = deposits.Select(x => x.Running).ToList();

            var applies = await _jewelryContext.TbtSaleOrderDepositApply
                .Where(x => depositRunnings.Contains(x.DepositRunning))
                .OrderBy(x => x.CreateDate)
                .ToListAsync();

            var appliesByDeposit = applies.ToLookup(x => x.DepositRunning);

            var saleOrder = await _jewelryContext.TbtSaleOrder
                .FirstOrDefaultAsync(x => x.SoNumber == request.SoNumber);

            var depositItems = deposits.Select(d =>
            {
                var depositApplies = appliesByDeposit[d.Running].ToList();
                var appliedAmount = depositApplies.Where(a => !a.IsDelete).Sum(a => a.Amount);

                return new jewelry.Model.Sale.SaleOrderDeposit.List.DepositItem
                {
                    Running = d.Running,
                    DepositDate = d.DepositDate,

                    Amount = d.Amount,
                    CurrencyUnit = d.CurrencyUnit,
                    CurrencyRate = d.CurrencyRate,

                    Payment = d.Payment,
                    PaymentName = d.PaymentName,
                    BankCode = d.BankCode,
                    BankBranch = d.BankBranch,
                    ReferenceNumber = d.ReferenceNumber,
                    ImagePath = d.ImagePath,
                    Remark = d.Remark,

                    IsDelete = d.IsDelete,
                    DeleteReason = d.DeleteReason,

                    AppliedAmount = appliedAmount,
                    RemainingAmount = d.Amount - appliedAmount,

                    CreateBy = d.CreateBy,
                    CreateDate = d.CreateDate,

                    Applies = depositApplies.Select(a => new jewelry.Model.Sale.SaleOrderDeposit.List.ApplyItem
                    {
                        InvoiceRunning = a.InvoiceRunning,
                        Amount = a.Amount,
                        IsDelete = a.IsDelete,
                        CreateDate = a.CreateDate
                    }).ToList()
                };
            }).ToList();

            var totalReceived = deposits.Where(x => !x.IsDelete).Sum(x => x.Amount);
            var totalApplied = applies.Where(x => !x.IsDelete).Sum(x => x.Amount);

            return new jewelry.Model.Sale.SaleOrderDeposit.List.Response
            {
                SoNumber = request.SoNumber,
                CurrencyUnit = saleOrder?.CurrencyUnit,
                TotalReceived = totalReceived,
                TotalApplied = totalApplied,
                Balance = totalReceived - totalApplied,
                Deposits = depositItems
            };
        }

        public async Task<string> Delete(jewelry.Model.Sale.SaleOrderDeposit.Delete.Request request)
        {
            if (string.IsNullOrEmpty(request.Running))
            {
                throw new HandleException("Running is Required.");
            }

            if (string.IsNullOrEmpty(request.DeleteReason))
            {
                throw new HandleException("Delete Reason is Required.");
            }

            var deposit = await _jewelryContext.TbtSaleOrderDeposit
                .FirstOrDefaultAsync(x => x.Running == request.Running);

            if (deposit == null)
            {
                throw new HandleException($"ไม่พบรายการมัดจำ {request.Running}");
            }

            using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
            try
            {
                // ล็อกแถวมัดจำของ SO นี้ทั้งหมด (รวมแถวที่กำลังจะลบ) ก่อนอ่านสถานะซ้ำ กันสองคำขอชนกัน
                await _jewelryContext.LockSaleOrderDepositsAsync(deposit.SoNumber);
                await _jewelryContext.Entry(deposit).ReloadAsync();

                if (deposit.IsDelete)
                {
                    throw new HandleException($"มัดจำ {request.Running} ถูกยกเลิกไปแล้ว");
                }

                var activeApplies = await _jewelryContext.TbtSaleOrderDepositApply
                    .Where(a => a.DepositRunning == request.Running && !a.IsDelete)
                    .ToListAsync();

                if (activeApplies.Any())
                {
                    var invoiceList = string.Join(", ", activeApplies.Select(a => a.InvoiceRunning).Distinct());
                    throw new HandleException($"มัดจำนี้ถูกหักเข้าใบแจ้งหนี้ {invoiceList} แล้ว ต้องยกเลิกใบแจ้งหนี้ก่อน");
                }

                deposit.IsDelete = true;
                deposit.DeleteReason = request.DeleteReason;
                deposit.UpdateBy = CurrentUsername;
                deposit.UpdateDate = DateTime.UtcNow;

                _jewelryContext.TbtSaleOrderDeposit.Update(deposit);

                await _jewelryContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (HandleException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new HandleException($"Error deleting deposit: {ex.Message}");
            }

            return $"ลบรายการมัดจำ {request.Running} เรียบร้อยแล้ว";
        }
    }
}
