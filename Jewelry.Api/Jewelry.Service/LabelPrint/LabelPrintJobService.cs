using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.LabelPrint
{
    public interface ILabelPrintJobService
    {
        Task<jewelry.Model.LabelPrint.Enqueue.Response> Enqueue(jewelry.Model.LabelPrint.Enqueue.Request request);
        IQueryable<jewelry.Model.LabelPrint.List.Response> List(jewelry.Model.LabelPrint.List.Search request);
        Task<jewelry.Model.LabelPrint.Claim.Response?> Claim(jewelry.Model.LabelPrint.Claim.Request request);
        Task<jewelry.Model.LabelPrint.Ack.Response> Ack(jewelry.Model.LabelPrint.Ack.Request request);
        Task<jewelry.Model.LabelPrint.Retry.Response> Retry(jewelry.Model.LabelPrint.Retry.Request request);
        Task<jewelry.Model.LabelPrint.Delete.Response> Delete(jewelry.Model.LabelPrint.Delete.Request request);
    }

    public class LabelPrintJobService : BaseService, ILabelPrintJobService
    {
        private readonly JewelryContext _jewelryContext;

        private const string StatusPending = "PENDING";
        private const string StatusPrinting = "PRINTING";
        private const string StatusPrinted = "PRINTED";
        private const string StatusFailed = "FAILED";
        private const string DefaultLabelType = "GEM";

        public LabelPrintJobService(JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public async Task<jewelry.Model.LabelPrint.Enqueue.Response> Enqueue(jewelry.Model.LabelPrint.Enqueue.Request request)
        {
            if (string.IsNullOrWhiteSpace(request?.ReferenceCode))
            {
                throw new HandleException("กรุณาระบุรหัสที่จะพิมพ์ป้าย");
            }
            if (string.IsNullOrWhiteSpace(request.Payload))
            {
                throw new HandleException("กรุณาระบุข้อมูลป้ายที่จะพิมพ์");
            }
            if (request.Copies < 1 || request.Copies > 100)
            {
                throw new HandleException("จำนวนป้ายต้องอยู่ระหว่าง 1 ถึง 100");
            }

            var job = new TbtLabelPrintJob
            {
                LabelType = string.IsNullOrWhiteSpace(request.LabelType) ? DefaultLabelType : request.LabelType.Trim(),
                ReferenceCode = request.ReferenceCode.Trim(),
                Payload = request.Payload,
                Copies = request.Copies,
                Status = StatusPending,
                RetryCount = 0,
                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,
            };

            _jewelryContext.TbtLabelPrintJob.Add(job);
            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.LabelPrint.Enqueue.Response
            {
                Id = job.Id,
            };
        }

        public IQueryable<jewelry.Model.LabelPrint.List.Response> List(jewelry.Model.LabelPrint.List.Search request)
        {
            var query = _jewelryContext.TbtLabelPrintJob.AsQueryable();

            if (request?.IncludeDeleted != true)
            {
                query = query.Where(x => !x.IsDeleted);
            }
            if (!string.IsNullOrEmpty(request?.ReferenceCode))
            {
                var code = request.ReferenceCode.Trim().ToLower();
                query = query.Where(x => x.ReferenceCode.ToLower().Contains(code));
            }
            if (!string.IsNullOrEmpty(request?.LabelType))
            {
                query = query.Where(x => x.LabelType == request.LabelType);
            }
            if (!string.IsNullOrEmpty(request?.CreateBy))
            {
                query = query.Where(x => x.CreateBy == request.CreateBy);
            }
            if (!string.IsNullOrEmpty(request?.Status))
            {
                query = query.Where(x => x.Status == request.Status);
            }
            if (request?.DateFrom != null)
            {
                query = query.Where(x => x.CreateDate >= request.DateFrom.Value.StartOfDayUtc());
            }
            if (request?.DateTo != null)
            {
                query = query.Where(x => x.CreateDate <= request.DateTo.Value.EndOfDayUtc());
            }

            return query
                .OrderByDescending(x => x.Id)
                .Select(x => new jewelry.Model.LabelPrint.List.Response
                {
                    Id = x.Id,
                    LabelType = x.LabelType,
                    ReferenceCode = x.ReferenceCode,
                    Payload = x.Payload,
                    Copies = x.Copies,
                    Status = x.Status,
                    ErrorMessage = x.ErrorMessage,
                    RetryCount = x.RetryCount,
                    StationId = x.StationId,
                    CreateBy = x.CreateBy,
                    CreateDate = x.CreateDate,
                    ClaimedDate = x.ClaimedDate,
                    PrintedDate = x.PrintedDate,
                    IsDeleted = x.IsDeleted,
                    DeletedBy = x.DeletedBy,
                    DeletedDate = x.DeletedDate,
                });
        }

        public async Task<jewelry.Model.LabelPrint.Claim.Response?> Claim(jewelry.Model.LabelPrint.Claim.Request request)
        {
            if (string.IsNullOrWhiteSpace(request?.StationId))
            {
                throw new HandleException("กรุณาระบุรหัสเครื่องพิมพ์");
            }

            var claimToken = Guid.NewGuid().ToString();

            int affected;

            if (string.IsNullOrWhiteSpace(request.LabelType))
            {
                affected = await _jewelryContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE tbt_label_print_job
                SET status = 'PRINTING', station_id = {request.StationId}, claim_token = {claimToken}, claimed_date = now()
                WHERE id = (
                    SELECT id FROM tbt_label_print_job
                    WHERE status = 'PENDING' AND is_deleted = false
                    ORDER BY id
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                )");
            }
            else
            {
                var labelType = request.LabelType.Trim();
                affected = await _jewelryContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE tbt_label_print_job
                SET status = 'PRINTING', station_id = {request.StationId}, claim_token = {claimToken}, claimed_date = now()
                WHERE id = (
                    SELECT id FROM tbt_label_print_job
                    WHERE status = 'PENDING' AND is_deleted = false AND label_type = {labelType}
                    ORDER BY id
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                )");
            }

            if (affected == 0)
            {
                return null;
            }

            var job = await _jewelryContext.TbtLabelPrintJob
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClaimToken == claimToken);

            if (job == null)
            {
                return null;
            }

            return new jewelry.Model.LabelPrint.Claim.Response
            {
                Id = job.Id,
                LabelType = job.LabelType,
                ReferenceCode = job.ReferenceCode,
                Payload = job.Payload,
                Copies = job.Copies,
                Status = job.Status,
                ErrorMessage = job.ErrorMessage,
                RetryCount = job.RetryCount,
                StationId = job.StationId,
                CreateBy = job.CreateBy,
                CreateDate = job.CreateDate,
                ClaimedDate = job.ClaimedDate,
                PrintedDate = job.PrintedDate,
            };
        }

        public async Task<jewelry.Model.LabelPrint.Ack.Response> Ack(jewelry.Model.LabelPrint.Ack.Request request)
        {
            var job = await _jewelryContext.TbtLabelPrintJob.FirstOrDefaultAsync(x => x.Id == request.Id);
            if (job == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            if (request.Success)
            {
                job.Status = StatusPrinted;
                job.PrintedDate = DateTime.UtcNow;
                job.ErrorMessage = null;
            }
            else
            {
                job.Status = StatusFailed;
                job.ErrorMessage = request.ErrorMessage;
            }

            _jewelryContext.TbtLabelPrintJob.Update(job);
            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.LabelPrint.Ack.Response
            {
                Id = job.Id,
                Status = job.Status,
                PrintedDate = job.PrintedDate,
            };
        }

        public async Task<jewelry.Model.LabelPrint.Retry.Response> Retry(jewelry.Model.LabelPrint.Retry.Request request)
        {
            var job = await _jewelryContext.TbtLabelPrintJob.FirstOrDefaultAsync(x => x.Id == request.Id);
            if (job == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            job.Status = StatusPending;
            job.RetryCount += 1;
            job.StationId = null;
            job.ClaimToken = null;
            job.ErrorMessage = null;

            _jewelryContext.TbtLabelPrintJob.Update(job);
            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.LabelPrint.Retry.Response
            {
                Id = job.Id,
                Status = job.Status,
                RetryCount = job.RetryCount,
            };
        }

        public async Task<jewelry.Model.LabelPrint.Delete.Response> Delete(jewelry.Model.LabelPrint.Delete.Request request)
        {
            var job = await _jewelryContext.TbtLabelPrintJob.FirstOrDefaultAsync(x => x.Id == request.Id);
            if (job == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            job.IsDeleted = true;
            job.DeletedBy = CurrentUsername;
            job.DeletedDate = DateTime.UtcNow;

            _jewelryContext.TbtLabelPrintJob.Update(job);
            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.LabelPrint.Delete.Response
            {
                Id = job.Id,
                IsDeleted = job.IsDeleted,
            };
        }
    }
}
