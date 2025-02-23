﻿using jewelry.Model.Exceptions;
using jewelry.Model.Worker;
using jewelry.Model.Worker.Create;
using jewelry.Model.Worker.List;
using jewelry.Model.Worker.Report;
using jewelry.Model.Worker.TrackingWorker;
using jewelry.Model.Worker.Update;
using jewelry.Model.Worker.WorkerWages;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
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
        ReportWorkerSummeryResponse SummeryReport(ReportWorkerWages request);
        IQueryable<TrackingWorkerResponse> TrackingWorker(TrackingWorker request);
    }
    public class WorkerService : IWorkerService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public WorkerService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
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

                CreateBy = _admin,
                CreateDate = DateTime.UtcNow,
                UpdateBy = _admin,
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
            dub.UpdateBy = _admin;

            _jewelryContext.TbmWorker.Update(dub);
            await _jewelryContext.SaveChangesAsync();

            return $"{request.Code.ToUpper()}-{request.NameTh}";
        }
        public IQueryable<ListWorkerProductionResponse> Search(ListWorkerProduction request)
        {
            var query = (from item in _jewelryContext.TbmWorker
                         join type in _jewelryContext.TbmProductionPlanStatus on item.TypeId equals type.Id into typeJoind
                         from tj in typeJoind.DefaultIfEmpty()
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
                         });

            if (!string.IsNullOrEmpty(request.Code))
            {
                query = (from item in query
                         where item.Code == request.Code.ToUpper()
                         select item);
            }
            if (!string.IsNullOrEmpty(request.Text))
            {
                query = (from item in query
                         where item.Code.Contains(request.Text.ToUpper())
                         || item.NameTh.Contains(request.Text)
                         || item.NameEn.Contains(request.Text)
                         select item);
            }
            if (request.Type.HasValue)
            {
                query = (from item in query
                         where item.Type == request.Type
                         select item);
            }
            if (request.Active.HasValue)
            {
                if (request.Active == 1)
                {
                    query = (from item in query
                             where item.IsActive == true
                             select item);
                }
                if (request.Active == 2)
                {
                    query = (from item in query
                             where item.IsActive == false
                             select item);
                }
            }

            return query.OrderByDescending(x => x.UpdateDate);
        }
        public SearchWorkerWagesResponse SearchWorkerWages(SearchWorkerWagesRequest request)
        {
            var requestDate = request.RequestDateStart.StartOfDayUtc();
            var query = (from item in _jewelryContext.TbtProductionPlanStatusDetail
                            .Include(x => x.Header)
                            .ThenInclude(x => x.ProductionPlan)
                            .ThenInclude(x => x.StatusNavigation)
                         join status in _jewelryContext.TbmProductionPlanStatus on item.Header.Status equals status.Id

                         where (item.Worker == request.Code.ToUpper() || item.WorkerSub == request.Code.ToUpper())
                         && item.RequestDate >= request.RequestDateStart.StartOfDayUtc()
                         && item.RequestDate <= request.RequestDateEnd.EndOfDayUtc()
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

                         }).ToList() // ดึงข้อมูลมาก่อน
                        .GroupBy(x => x.WoText)
                        .OrderBy(g => g.Min(x => x.JobDate))
                        .SelectMany(g => g.OrderBy(x => x.JobDate)
                        .ThenByDescending(x => x.Gold)
                        .ThenBy(x => x.ItemNo));

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


            return query;

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

           return new ReportWorkerSummeryResponse()
           {
                TotalGoldQtySend = query.Sum(x => x.GoldQtySend),
                TotalGoldWeightSend = query.Sum(x => x.GoldWeightSend),
                TotalGoldQtyCheck = query.Sum(x => x.GoldQtyCheck),
                TotalGoldWeightCheck = query.Sum(x => x.GoldWeightCheck),
                TotalWages = query.Sum(x => x.TotalWages),
            };

        }

    }
}
