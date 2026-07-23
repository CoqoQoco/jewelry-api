using jewelry.Model.Exceptions;
using jewelry.Model.Worker;
using jewelry.Model.Worker.Create;
using jewelry.Model.Worker.List;
using jewelry.Model.Worker.Report;
using jewelry.Model.Worker.TrackingWorker;
using jewelry.Model.Worker.Update;
using jewelry.Model.Worker.WorkerWages;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NPOI.OpenXmlFormats.Dml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Worker
{
    public interface IWorkerService
    {
        IQueryable<MasterWorkerProductionTypeResponse> GetWorkerProductionType();
        Task<string> Create(CreateProductionWorkerRequest request);
        Task<string> Update(UpdateProductionWorkerRequest request);
        IQueryable<ListWorkerProductionResponse> Search(ListWorkerProduction request);
        SearchWorkerWagesResponse SearchWorkerWages(SearchWorkerWagesRequest request);
        SearchWorkerWagesResponse SearchWorkerActiveStatus(SearchWorkerWagesRequest request);
        IQueryable<ReportWorkerWagesResponse> Report(ReportWorkerWages request);
        IQueryable<ReportWorkerWagesByWorkerResponse> ReportByWorker(ReportWorkerWages request);
        ReportWorkerSummeryResponse SummeryReport(ReportWorkerWages request);
        IQueryable<TrackingWorkerResponse> TrackingWorker(TrackingWorker request);
        jewelry.Model.Worker.WagesByProcess.SearchResponse WagesByProcess(jewelry.Model.Worker.WagesByProcess.SearchRequest request);
        jewelry.Model.Worker.WagesMonthlyTrend.SearchResponse WagesMonthlyTrend(jewelry.Model.Worker.WagesMonthlyTrend.SearchRequest request);
    }
    public class WorkerService : BaseService, IWorkerService
    {
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public WorkerService(JewelryContext JewelryContext, 
            IHttpContextAccessor httpContextAccessor, 
            IHostEnvironment HostingEnvironment) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
        }

        public IQueryable<MasterWorkerProductionTypeResponse> GetWorkerProductionType()
        {
            var getId = new int[] { 50, 60, 80,70, 90 };
            var query = (from item in _jewelryContext.TbmProductionPlanStatus
                         where getId.Contains(item.Id)
                         select new MasterWorkerProductionTypeResponse()
                         {
                             Id = item.Id,
                             NameEn = item.NameEn,
                             NameTh = item.NameTh,
                             Description = item.Description ?? null
                         });

            return query;
        }

        public async Task<string> Create(CreateProductionWorkerRequest request)
        {
            var dub = (from item in _jewelryContext.TbmWorker
                       where item.Code == request.Code.ToUpper()
                       select item).SingleOrDefault();

            if (dub != null)
            {
                throw new HandleException($"พบรหัสพนักงาน {request.Code} ซ้ำในระบบ กรุณาสร้างรหัสใหม่");
            }

            var add = new TbmWorker()
            {
                Code = request.Code.ToUpper(),
                NameEn = request.NameEn,
                NameTh = request.NameTh,
                TypeId = request.Type,
                IsActive = true,

                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,
                UpdateBy = CurrentUsername,
                UpdateDate = DateTime.UtcNow,
            };

            _jewelryContext.TbmWorker.Add(add);
            await _jewelryContext.SaveChangesAsync();

            return $"{request.Code.ToUpper()}-{request.NameTh}";
        }
        public async Task<string> Update(UpdateProductionWorkerRequest request)
        {
            var dub = (from item in _jewelryContext.TbmWorker
                       where item.Code == request.Code.ToUpper()
                       select item).SingleOrDefault();

            if (dub == null)
            {
                throw new HandleException($" ไม่พบพบรหัสพนักงาน {request.Code} ซ้ำในระบบ กรุณาสร้างรหัสใหม่");
            }

            dub.NameEn = request.NameEn;
            dub.NameTh = request.NameTh;
            dub.TypeId = request.Type;
            dub.UpdateDate = DateTime.UtcNow;
            dub.UpdateBy = CurrentUsername;

            _jewelryContext.TbmWorker.Update(dub);
            await _jewelryContext.SaveChangesAsync();

            return $"{request.Code.ToUpper()}-{request.NameTh}";
        }
        public IQueryable<ListWorkerProductionResponse> Search(ListWorkerProduction request)
        {
            var workers = _jewelryContext.TbmWorker.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(request.Code))
            {
                var code = request.Code.ToUpper();
                workers = workers.Where(w => w.Code == code);
            }
            if (!string.IsNullOrEmpty(request.Text))
            {
                var textUpper = request.Text.ToUpper();
                var text = request.Text;
                workers = workers.Where(w =>
                    w.Code.Contains(textUpper)
                    || w.NameTh.Contains(text)
                    || (w.NameEn != null && w.NameEn.Contains(text)));
            }
            if (request.Type.HasValue)
            {
                workers = workers.Where(w => w.TypeId == request.Type);
            }
            if (request.Active.HasValue)
            {
                if (request.Active == 1) workers = workers.Where(w => w.IsActive == true);
                else if (request.Active == 2) workers = workers.Where(w => w.IsActive == false);
            }

            var query = from item in workers
                        join type in _jewelryContext.TbmProductionPlanStatus.AsNoTracking()
                            on item.TypeId equals type.Id into typeJoined
                        from tj in typeJoined.DefaultIfEmpty()
                        select new ListWorkerProductionResponse()
                        {
                            Code = item.Code,
                            NameEn = item.NameEn,
                            NameTh = item.NameTh,
                            Type = item.TypeId,
                            TypeName = tj.Description,
                            IsActive = item.IsActive,
                            CreateDate = item.CreateDate,
                            CreateBy = item.CreateBy,
                            UpdateDate = item.UpdateDate,
                            UpdateBy = item.UpdateBy,
                        };

            return query;
        }
        public SearchWorkerWagesResponse SearchWorkerWages(SearchWorkerWagesRequest request)
        {
            var startUtc = request.RequestDateStart.StartOfDayUtc();
            var endUtc = request.RequestDateEnd.EndOfDayUtc();

            var allItems = (from item in _jewelryContext.TbtProductionPlanStatusDetail
                            .Include(x => x.Header)
                            .ThenInclude(x => x.ProductionPlan)
                            .ThenInclude(x => x.StatusNavigation)
                         join status in _jewelryContext.TbmProductionPlanStatus on item.Header.Status equals status.Id

                         where (item.Worker == request.Code.ToUpper() || item.WorkerSub == request.Code.ToUpper())
                         && item.RequestDate >= startUtc
                         && item.RequestDate <= endUtc
                         && item.IsActive == true
                         && item.Header.IsActive == true

                         select new SearchWorkerWages()
                         {
                             Wo = item.Header.ProductionPlan.Wo,
                             WoNumber = item.Header.ProductionPlan.WoNumber,
                             WoText = item.Header.ProductionPlan.WoText,
                             ProductNumber = item.Header.ProductionPlan.ProductNumber,
                             ProductName = item.Header.ProductionPlan.ProductName,

                             StatusActive = item.Header.ProductionPlan.Status,
                             StatusActiveName = item.Header.ProductionPlan.StatusNavigation.NameTh,
                             Status = status.Id,
                             StatusName = status.NameTh,
                             StatusDescription = status.Description,

                             Gold = item.Gold,
                             GoldSize = item.Header.ProductionPlan.TypeSize,

                             GoldQtySend = item.GoldQtySend,
                             GoldWeightSend = item.GoldWeightSend,
                             GoldQtyCheck = item.GoldQtyCheck,
                             GoldWeightCheck = item.GoldWeightCheck,

                             Description = item.Description,
                             Wages = item.Wages,
                             TotalWages = item.TotalWages,
                             WagesStatus = item.Header.ProductionPlan.Status == status.Id ? 10 : 100,

                             JobDate = item.RequestDate,
                             ItemNo = item.ItemNo

                         }).ToList();

            var goldLossRows = (from item in _jewelryContext.TbtProductionPlanStatusDetail
                                    .Include(x => x.Header)
                                    .ThenInclude(x => x.ProductionPlan)
                                    .ThenInclude(x => x.StatusNavigation)
                                join status in _jewelryContext.TbmProductionPlanStatus on item.Header.Status equals status.Id
                                join slip in _jewelryContext.TbtWorkerGoldLossSlip on item.WorkerGoldLossSlipId equals slip.Id into slipJoined
                                from slipJ in slipJoined.DefaultIfEmpty()
                                where item.IsActive
                                   && item.Header.IsActive
                                   && item.Header.Status == 80
                                   && (item.Header.WorkerCode == request.Code.ToUpper()
                                       || item.Worker == request.Code.ToUpper()
                                       || item.WorkerSub == request.Code.ToUpper())
                                   && item.RequestDate >= startUtc
                                   && item.RequestDate <= endUtc
                                   && item.LossPercent != null
                                select new
                                {
                                    ItemId = item.ProductionPlanId,
                                    item.ItemNo,
                                    item.Header.ProductionPlan.Wo,
                                    item.Header.ProductionPlan.WoNumber,
                                    item.Header.ProductionPlan.WoText,
                                    item.Header.ProductionPlan.ProductNumber,
                                    item.Header.ProductionPlan.ProductName,
                                    StatusActive = item.Header.ProductionPlan.Status,
                                    StatusActiveName = item.Header.ProductionPlan.StatusNavigation.NameTh,
                                    StatusDescription = status.Description,
                                    GoldSize = item.Header.ProductionPlan.TypeSize,
                                    item.RequestDate,
                                    item.Gold,
                                    item.GoldQtySend,
                                    item.GoldWeightSend,
                                    item.GoldQtyCheck,
                                    item.GoldWeightCheck,
                                    item.Description,
                                    item.LossPercent,
                                    item.LossRemark,
                                    GoldLossPrice = item.Header.GoldLossPrice,
                                    item.WorkerGoldLossSlipId,
                                    WorkerGoldLossSlipDocumentNo = slipJ != null ? slipJ.DocumentNo : null,
                                })
                                .ToList()
                                .Select(x =>
                                {
                                    var weightDiff = (x.GoldWeightSend ?? 0) - (x.GoldWeightCheck ?? 0);
                                    var weightLossAllowed = (x.GoldWeightCheck ?? 0) * (x.LossPercent ?? 0) / 100m;
                                    var weightLossActual = Math.Round(weightLossAllowed - weightDiff, 4, MidpointRounding.ToPositiveInfinity);
                                    var moneyDiff = Math.Round(weightLossActual, 2, MidpointRounding.ToPositiveInfinity) * (x.GoldLossPrice ?? 0);

                                    return new SearchWorkerWages
                                    {
                                        Id = x.ItemId,
                                        Wo = x.Wo,
                                        WoNumber = x.WoNumber,
                                        WoText = x.WoText,
                                        ProductNumber = x.ProductNumber,
                                        ProductName = x.ProductName,
                                        GoldSize = x.GoldSize,
                                        StatusActive = x.StatusActive,
                                        StatusActiveName = x.StatusActiveName,
                                        StatusDescription = x.StatusDescription,
                                        JobDate = x.RequestDate,
                                        Gold = x.Gold,
                                        GoldQtySend = x.GoldQtySend,
                                        GoldWeightSend = x.GoldWeightSend,
                                        GoldQtyCheck = x.GoldQtyCheck,
                                        GoldWeightCheck = x.GoldWeightCheck,
                                        Description = x.Description,
                                        StatusName = "Gold Loss",
                                        Status = 80,
                                        Wages = null,
                                        TotalWages = moneyDiff,
                                        IsGoldLoss = true,
                                        LossPercent = x.LossPercent,
                                        LossRemark = x.LossRemark,
                                        GoldLossPrice = x.GoldLossPrice,
                                        ItemNo = x.ItemNo,
                                        WorkerGoldLossSlipId = x.WorkerGoldLossSlipId,
                                        WorkerGoldLossSlipDocumentNo = x.WorkerGoldLossSlipDocumentNo,
                                    };
                                })
                                .ToList();

            allItems.AddRange(goldLossRows);

            var sorted = allItems
                .GroupBy(x => x.WoText)
                .OrderBy(g => g.Min(x => x.JobDate))
                .ThenBy(x => x.Key)
                .SelectMany(g => g.OrderBy(x => x.JobDate)
                    .ThenByDescending(x => x.Gold)
                    .ThenBy(x => x.ItemNo))
                .ToList();

            var response = new SearchWorkerWagesResponse()
            {
                WagesDateStart = request.RequestDateStart.UtcDateTime,
                WagesDateEnd = request.RequestDateEnd.UtcDateTime,

                TotalGoldQtySend = sorted.Sum(x => x.GoldQtySend),
                TotalGoldWeightSend = sorted.Sum(x => x.GoldWeightSend),
                TotalGoldQtyCheck = sorted.Sum(x => x.GoldQtyCheck),
                TotalGoldWeightCheck = sorted.Sum(x => x.GoldWeightCheck),

                TotalWages = sorted.Sum(x => x.TotalWages),
                Items = sorted,
            };

            return response;
        }
        public SearchWorkerWagesResponse SearchWorkerActiveStatus(SearchWorkerWagesRequest request)
        {
            var query = (from item in _jewelryContext.TbtProductionPlanStatusDetail
                            .Include(x => x.Header)
                            .ThenInclude(x => x.ProductionPlan)
                         join status in _jewelryContext.TbmProductionPlanStatus on item.Header.Status equals status.Id

                         where item.Worker == request.Code.ToUpper()
                         && item.RequestDate>= request.RequestDateStart.StartOfDayUtc()
                         && item.RequestDate <= request.RequestDateEnd.EndOfDayUtc()
                         && item.IsActive == true
                         && item.Header.IsActive == true
                         && status.Id == item.Header.Status

                         select new SearchWorkerWages()
                         {
                             Wo = item.Header.ProductionPlan.Wo,
                             WoNumber = item.Header.ProductionPlan.WoNumber,
                             WoText = item.Header.ProductionPlan.WoText,
                             ProductNumber = item.Header.ProductionPlan.ProductNumber,
                             ProductName = item.Header.ProductionPlan.ProductName,

                             Status = status.Id,
                             StatusName = status.NameTh,
                             StatusDescription = status.Description,

                             Gold = item.Gold,

                             GoldQtySend = item.GoldQtySend,
                             GoldWeightSend = item.GoldWeightSend,
                             GoldQtyCheck = item.GoldQtyCheck,
                             GoldWeightCheck = item.GoldWeightCheck,

                             Description = item.Description,
                             Wages = item.Wages,
                             TotalWages = item.TotalWages,
                             WagesStatus = item.Wages.HasValue && item.Wages.Value > 0 ? 100 : 10,

                             JobDate = item.RequestDate,

                         }).OrderByDescending(x => x.WagesStatus).ThenBy(x => x.JobDate).ThenBy(x => x.WoText).ThenByDescending(x => x.Gold);

            var response = new SearchWorkerWagesResponse()
            {
                WagesDateStart = request.RequestDateStart.UtcDateTime,
                WagesDateEnd = request.RequestDateEnd.UtcDateTime,

                TotalGoldQtySend = query.Sum(x => x.GoldQtySend),
                TotalGoldWeightSend = query.Sum(x => x.GoldWeightSend),
                TotalGoldQtyCheck = query.Sum(x => x.GoldQtyCheck),
                TotalGoldWeightCheck = query.Sum(x => x.GoldWeightCheck),

                TotalWages = query.Sum(x => x.TotalWages),
                Items = query.ToList(),
            };

            return response;
        }
        public IQueryable<ReportWorkerWagesResponse> Report(ReportWorkerWages request)
        {
            //var statusCheck = new int[] { 50, 60,70, 80, 90 };
            var query = (from item in _jewelryContext.TbtProductionPlanStatusDetail
                         .Include(x => x.Header)
                         .ThenInclude(x => x.ProductionPlan)
                         join status in _jewelryContext.TbmProductionPlanStatus on item.Header.Status equals status.Id
                         join worker in _jewelryContext.TbmWorker on item.Worker equals worker.Code

                         //where item.Worker == request.Code.ToUpper()
                         where item.RequestDate >= request.CreateStart.StartOfDayUtc()
                         && item.RequestDate <= request.CreateEnd.EndOfDayUtc()
                         && item.IsActive == true
                         && item.Header.IsActive == true
                         //&& item.Wages.HasValue && item.Wages.Value > 0

                         select new ReportWorkerWagesResponse()
                         {
                             Wo = item.Header.ProductionPlan.Wo,
                             WoNumber = item.Header.ProductionPlan.WoNumber,
                             WoText = item.Header.ProductionPlan.WoText,
                             ProductNumber = item.Header.ProductionPlan.ProductNumber,
                             ProductName = item.Header.ProductionPlan.ProductName,

                             WorkerCode = item.Worker,
                             WorkerName = worker.NameTh,

                             Status = status.Id,
                             StatusName = status.NameTh,
                             StatusDescription = status.Description,

                             Gold = item.Gold,
                             GoldSize = item.Header.ProductionPlan.TypeSize,
                             Mold = item.Header.ProductionPlan.Mold,

                             GoldQtySend = item.GoldQtySend,
                             GoldWeightSend = item.GoldWeightSend,
                             GoldQtyCheck = item.GoldQtyCheck,
                             GoldWeightCheck = item.GoldWeightCheck,

                             Description = item.Description,
                             Wages = item.Wages,
                             TotalWages = item.TotalWages,
                             WagesStatus = item.Wages.HasValue && item.Wages.Value > 0 ? 100 : 10,

                             JobDate = item.RequestDate,
                         });

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query
                         where item.WorkerCode.Contains(request.Text.ToUpper())
                         || item.WorkerName.Contains(request.Text)
                         || item.ProductNumber.Contains(request.Text)
                         || item.ProductName.Contains(request.Text)
                         //|| item.WoText.Contains(request.Text)
                         select item);
            }
            if (!string.IsNullOrEmpty(request.WoText))
            {
                query = (from item in query
                         where item.WoText.Contains(request.WoText.ToString())
                         select item);
            }
            if (!string.IsNullOrEmpty(request.WorkerCode))
            {
                query = query.Where(x => x.WorkerCode == request.WorkerCode);
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.ProductNumber != null && x.ProductNumber.Contains(request.ProductNumber));
            }
            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => x.Gold != null && request.Gold.Any(g => x.Gold.Contains(g)));
            }
            if (request.GoldSize != null && request.GoldSize.Any())
            {
                query = query.Where(x => x.GoldSize != null && request.GoldSize.Any(g => x.GoldSize.Contains(g)));
            }
            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }
            if (!string.IsNullOrEmpty(request.Mold))
            {
                query = query.Where(x => x.Mold != null && x.Mold.Contains(request.Mold));
            }


            return query;

        }

        public IQueryable<ReportWorkerWagesByWorkerResponse> ReportByWorker(ReportWorkerWages request)
        {
            return this.Report(request)
                .GroupBy(x => new { x.WorkerCode, x.WorkerName })
                .Select(g => new ReportWorkerWagesByWorkerResponse
                {
                    WorkerCode = g.Key.WorkerCode,
                    WorkerName = g.Key.WorkerName,
                    JobCount = g.Count(),
                    TotalQty = g.Sum(x => x.GoldQtyCheck ?? x.GoldQtySend),
                    TotalWages = g.Sum(x => x.TotalWages)
                });
        }

        public IQueryable<TrackingWorkerResponse> TrackingWorker(TrackingWorker request)
        {
            //var statusCheck = new int[] { 50, 60,70, 80, 90 };
            var query = (from item in _jewelryContext.TbtProductionPlanStatusDetail
                         .Include(x => x.Header)
                         .ThenInclude(x => x.ProductionPlan)
                         join status in _jewelryContext.TbmProductionPlanStatus on item.Header.Status equals status.Id
                         join worker in _jewelryContext.TbmWorker on item.Worker equals worker.Code

                         where item.IsActive == true
                         && item.Header.IsActive == true

                         select new TrackingWorkerResponse()
                         {
                             Wo = item.Header.ProductionPlan.Wo,
                             WoNumber = item.Header.ProductionPlan.WoNumber,
                             WoText = item.Header.ProductionPlan.WoText,
                             ProductNumber = item.Header.ProductionPlan.ProductNumber,
                             ProductName = item.Header.ProductionPlan.ProductName,
                             Mold = item.Header.ProductionPlan.Mold,

                             WorkerCode = item.Worker,
                             WorkerName = worker.NameTh,

                             Status = status.Id,
                             StatusName = status.NameTh,
                             StatusDescription = status.Description,

                             Gold = item.Gold,

                             GoldQtySend = item.GoldQtySend,
                             GoldWeightSend = item.GoldWeightSend,
                             GoldQtyCheck = item.GoldQtyCheck,
                             GoldWeightCheck = item.GoldWeightCheck,

                             Description = item.Description,
                             Wages = item.Wages,
                             TotalWages = item.TotalWages,
                             WagesStatus = item.Wages.HasValue && item.Wages.Value > 0 ? 100 : 10,

                             JobDate = item.RequestDate,
                         });

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query
                         where item.WorkerCode.Contains(request.Text.ToUpper())
                         || item.WorkerName.Contains(request.Text)
                         || item.ProductNumber.Contains(request.Text)
                         || item.ProductName.Contains(request.Text)
                         || item.WoText.Contains(request.Text)
                         select item);
            }

            if (request.CreateStart.HasValue)
            {
                query = query.Where(x => x.JobDate >= request.CreateStart.Value.StartOfDayUtc());
            }
            if (request.CreateEnd.HasValue)
            {
                query = query.Where(x => x.JobDate <= request.CreateEnd.Value.EndOfDayUtc());
            }
            return query;
        }

        public ReportWorkerSummeryResponse SummeryReport(ReportWorkerWages request)
        {
            //var statusCheck = new int[] { 50, 60,70, 80, 90 };
            var query = (from item in _jewelryContext.TbtProductionPlanStatusDetail
                         .Include(x => x.Header)
                         .ThenInclude(x => x.ProductionPlan)
                         join status in _jewelryContext.TbmProductionPlanStatus on item.Header.Status equals status.Id
                         join worker in _jewelryContext.TbmWorker on item.Worker equals worker.Code

                         //where item.Worker == request.Code.ToUpper()
                         where item.RequestDate >= request.CreateStart.StartOfDayUtc()
                         && item.RequestDate <= request.CreateEnd.EndOfDayUtc()
                         && item.IsActive == true
                         && item.Header.IsActive == true
                         //&& item.Wages.HasValue && item.Wages.Value > 0

                         select new ReportWorkerWagesResponse()
                         {
                             Wo = item.Header.ProductionPlan.Wo,
                             WoNumber = item.Header.ProductionPlan.WoNumber,
                             WoText = item.Header.ProductionPlan.WoText,
                             ProductNumber = item.Header.ProductionPlan.ProductNumber,
                             ProductName = item.Header.ProductionPlan.ProductName,

                             WorkerCode = item.Worker,
                             WorkerName = worker.NameTh,

                             Status = status.Id,
                             StatusName = status.NameTh,
                             StatusDescription = status.Description,

                             Gold = item.Gold,
                             GoldSize = item.Header.ProductionPlan.TypeSize,
                             Mold = item.Header.ProductionPlan.Mold,

                             GoldQtySend = item.GoldQtySend,
                             GoldWeightSend = item.GoldWeightSend,
                             GoldQtyCheck = item.GoldQtyCheck,
                             GoldWeightCheck = item.GoldWeightCheck,

                             Description = item.Description,
                             Wages = item.Wages,
                             TotalWages = item.TotalWages,
                             WagesStatus = item.Wages.HasValue && item.Wages.Value > 0 ? 100 : 10,

                             JobDate = item.RequestDate,
                         });

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query
                         where item.WorkerCode.Contains(request.Text.ToUpper())
                         || item.WorkerName.Contains(request.Text)
                         || item.ProductNumber.Contains(request.Text)
                         || item.ProductName.Contains(request.Text)
                         //|| item.WoText.Contains(request.Text)
                         select item);
            }
            if (!string.IsNullOrEmpty(request.WoText))
            {
                query = (from item in query
                         where item.WoText.Contains(request.WoText.ToString())
                         select item);
            }
            if (!string.IsNullOrEmpty(request.WorkerCode))
            {
                query = query.Where(x => x.WorkerCode == request.WorkerCode);
            }
            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.ProductNumber != null && x.ProductNumber.Contains(request.ProductNumber));
            }
            if (request.Gold != null && request.Gold.Any())
            {
                query = query.Where(x => x.Gold != null && request.Gold.Any(g => x.Gold.Contains(g)));
            }
            if (request.GoldSize != null && request.GoldSize.Any())
            {
                query = query.Where(x => x.GoldSize != null && request.GoldSize.Any(g => x.GoldSize.Contains(g)));
            }
            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }
            if (!string.IsNullOrEmpty(request.Mold))
            {
                query = query.Where(x => x.Mold != null && x.Mold.Contains(request.Mold));
            }

           return new ReportWorkerSummeryResponse()
           {
                TotalGoldQtySend = query.Sum(x => x.GoldQtySend),
                TotalGoldWeightSend = query.Sum(x => x.GoldWeightSend),
                TotalGoldQtyCheck = query.Sum(x => x.GoldQtyCheck),
                TotalGoldWeightCheck = query.Sum(x => x.GoldWeightCheck),
                TotalWages = query.Sum(x => x.TotalWages),
            };

        }

        public jewelry.Model.Worker.WagesByProcess.SearchResponse WagesByProcess(jewelry.Model.Worker.WagesByProcess.SearchRequest request)
        {
            var query = from detail in _jewelryContext.TbtProductionPlanStatusDetail
                        join header in _jewelryContext.TbtProductionPlanStatusHeader
                            on detail.HeaderId equals header.Id
                        join status in _jewelryContext.TbmProductionPlanStatus
                            on header.Status equals status.Id
                        where detail.IsActive == true
                        && header.IsActive == true
                        select new { detail, header, status };

            if (request.Start.HasValue)
            {
                var start = request.Start.Value.StartOfDayUtc();
                query = query.Where(x => x.detail.RequestDate >= start);
            }
            if (request.End.HasValue)
            {
                var end = request.End.Value.EndOfDayUtc();
                query = query.Where(x => x.detail.RequestDate <= end);
            }
            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.header.Status));
            }

            var grouped = query
                .GroupBy(x => new { x.header.Status, x.status.NameTh })
                .Select(g => new
                {
                    StatusCode = g.Key.Status,
                    StatusName = g.Key.NameTh,
                    JobCount = g.Count(),
                    TotalWages = g.Sum(x => x.detail.TotalWages ?? 0)
                })
                .ToList();

            var rows = grouped
                .Select(x => new jewelry.Model.Worker.WagesByProcess.WagesByProcessRow
                {
                    StatusCode = x.StatusCode,
                    StatusName = x.StatusName ?? string.Empty,
                    JobCount = x.JobCount,
                    TotalWages = x.TotalWages,
                    AvgWagesPerJob = x.JobCount > 0 ? Math.Round(x.TotalWages / x.JobCount, 2) : 0
                })
                .OrderByDescending(x => x.TotalWages)
                .ToList();

            var totalJobCount = rows.Sum(x => x.JobCount);
            var totalWages = rows.Sum(x => x.TotalWages);

            var total = new jewelry.Model.Worker.WagesByProcess.WagesByProcessTotal
            {
                JobCount = totalJobCount,
                TotalWages = totalWages,
                AvgWagesPerJob = totalJobCount > 0 ? Math.Round(totalWages / totalJobCount, 2) : 0
            };

            return new jewelry.Model.Worker.WagesByProcess.SearchResponse
            {
                Rows = rows,
                Total = total
            };
        }

        public jewelry.Model.Worker.WagesMonthlyTrend.SearchResponse WagesMonthlyTrend(jewelry.Model.Worker.WagesMonthlyTrend.SearchRequest request)
        {
            var query = _jewelryContext.TbtProductionPlanStatusDetail
                .Where(x => x.IsActive && x.RequestDate != null)
                .AsQueryable();

            if (request.Start.HasValue)
            {
                var start = request.Start.Value.StartOfDayUtc();
                query = query.Where(x => x.RequestDate >= start);
            }
            if (request.End.HasValue)
            {
                var end = request.End.Value.EndOfDayUtc();
                query = query.Where(x => x.RequestDate <= end);
            }

            var grouped = query
                .GroupBy(x => new { x.RequestDate.Value.Year, x.RequestDate.Value.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    JobCount = g.Count(),
                    TotalWages = g.Sum(x => x.TotalWages ?? 0)
                })
                .ToList();

            var rows = grouped
                .Select(x => new jewelry.Model.Worker.WagesMonthlyTrend.WagesMonthlyTrendRow
                {
                    Year = x.Year,
                    Month = x.Month,
                    Ym = $"{x.Year:D4}-{x.Month:D2}",
                    JobCount = x.JobCount,
                    TotalWages = x.TotalWages,
                    AvgWagesPerJob = x.JobCount > 0 ? Math.Round(x.TotalWages / x.JobCount, 2) : 0
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            var totalJobCount = rows.Sum(x => x.JobCount);
            var totalWages = rows.Sum(x => x.TotalWages);

            var total = new jewelry.Model.Worker.WagesMonthlyTrend.WagesMonthlyTrendTotal
            {
                JobCount = totalJobCount,
                TotalWages = totalWages,
                AvgWagesPerJob = totalJobCount > 0 ? Math.Round(totalWages / totalJobCount, 2) : 0
            };

            return new jewelry.Model.Worker.WagesMonthlyTrend.SearchResponse
            {
                Rows = rows,
                Total = total
            };
        }

    }
}
