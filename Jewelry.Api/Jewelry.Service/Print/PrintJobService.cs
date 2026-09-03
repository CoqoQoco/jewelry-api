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

namespace Jewelry.Service.Print
{
    public interface IPrintJobService
    {
        Task<jewelry.Model.Print.Enqueue.Response> Enqueue(jewelry.Model.Print.Enqueue.Request request);
        IQueryable<jewelry.Model.Print.List.Response> List(jewelry.Model.Print.List.Search request);
        Task<jewelry.Model.Print.Claim.Response?> Claim(jewelry.Model.Print.Claim.Request request);
        Task<jewelry.Model.Print.Ack.Response> Ack(jewelry.Model.Print.Ack.Request request);
        Task<jewelry.Model.Print.Retry.Response> Retry(jewelry.Model.Print.Retry.Request request);
    }

    public class PrintJobService : BaseService, IPrintJobService
    {
        private readonly JewelryContext _jewelryContext;

        private const string StatusPending = "PENDING";
        private const string StatusPrinting = "PRINTING";
        private const string StatusPrinted = "PRINTED";
        private const string StatusFailed = "FAILED";

        public PrintJobService(JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public async Task<jewelry.Model.Print.Enqueue.Response> Enqueue(jewelry.Model.Print.Enqueue.Request request)
        {
            if (string.IsNullOrWhiteSpace(request?.InvoiceNumber))
            {
                throw new HandleException("กรุณาระบุเลขที่ใบเสร็จ");
            }
            if (string.IsNullOrWhiteSpace(request.Payload))
            {
                throw new HandleException("กรุณาระบุข้อมูลใบเสร็จที่จะพิมพ์");
            }

            var job = new TbtPrintJob
            {
                InvoiceNumber = request.InvoiceNumber,
                Payload = request.Payload,
                Status = StatusPending,
                RetryCount = 0,
                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,
            };

            _jewelryContext.TbtPrintJob.Add(job);
            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.Print.Enqueue.Response
            {
                Id = job.Id,
            };
        }

        public IQueryable<jewelry.Model.Print.List.Response> List(jewelry.Model.Print.List.Search request)
        {
            var query = _jewelryContext.TbtPrintJob.AsQueryable();

            if (!string.IsNullOrEmpty(request?.InvoiceNumber))
            {
                query = query.Where(x => x.InvoiceNumber == request.InvoiceNumber);
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
                .Select(x => new jewelry.Model.Print.List.Response
                {
                    Id = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    Payload = x.Payload,
                    Status = x.Status,
                    ErrorMessage = x.ErrorMessage,
                    RetryCount = x.RetryCount,
                    StationId = x.StationId,
                    CreateBy = x.CreateBy,
                    CreateDate = x.CreateDate,
                    ClaimedDate = x.ClaimedDate,
                    PrintedDate = x.PrintedDate,
                });
        }

        public async Task<jewelry.Model.Print.Claim.Response?> Claim(jewelry.Model.Print.Claim.Request request)
        {
            if (string.IsNullOrWhiteSpace(request?.StationId))
            {
                throw new HandleException("กรุณาระบุรหัสเครื่องพิมพ์");
            }

            var claimToken = Guid.NewGuid().ToString();

            var affected = await _jewelryContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE tbt_print_job
                SET status = 'PRINTING', station_id = {request.StationId}, claim_token = {claimToken}, claimed_date = now()
                WHERE id = (
                    SELECT id FROM tbt_print_job
                    WHERE status = 'PENDING'
                    ORDER BY id
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                )");

            if (affected == 0)
            {
                return null;
            }

            var job = await _jewelryContext.TbtPrintJob
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClaimToken == claimToken);

            if (job == null)
            {
                return null;
            }

            return new jewelry.Model.Print.Claim.Response
            {
                Id = job.Id,
                InvoiceNumber = job.InvoiceNumber,
                Payload = job.Payload,
                Status = job.Status,
                RetryCount = job.RetryCount,
                StationId = job.StationId,
                CreateDate = job.CreateDate,
                ClaimedDate = job.ClaimedDate,
            };
        }

        public async Task<jewelry.Model.Print.Ack.Response> Ack(jewelry.Model.Print.Ack.Request request)
        {
            var job = await _jewelryContext.TbtPrintJob.FirstOrDefaultAsync(x => x.Id == request.Id);
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

            _jewelryContext.TbtPrintJob.Update(job);
            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.Print.Ack.Response
            {
                Id = job.Id,
                Status = job.Status,
                PrintedDate = job.PrintedDate,
            };
        }

        public async Task<jewelry.Model.Print.Retry.Response> Retry(jewelry.Model.Print.Retry.Request request)
        {
            var job = await _jewelryContext.TbtPrintJob.FirstOrDefaultAsync(x => x.Id == request.Id);
            if (job == null)
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            job.Status = StatusPending;
            job.RetryCount += 1;
            job.StationId = null;
            job.ClaimToken = null;
            job.ErrorMessage = null;

            _jewelryContext.TbtPrintJob.Update(job);
            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.Print.Retry.Response
            {
                Id = job.Id,
                Status = job.Status,
                RetryCount = job.RetryCount,
            };
        }
    }
}
