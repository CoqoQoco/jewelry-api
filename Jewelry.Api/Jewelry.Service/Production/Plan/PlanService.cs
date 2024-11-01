using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using jewelry.Model.Production.Plan.Transfer;
using jewelry.Model.ProductionPlan.ProductionPlanStatus.Transfer;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Production.Plan
{
    public class PlanService : IPlanService
    {
        private readonly string _admin = "@ADMIN";

        private readonly JewelryContext _jewelryContext;

        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public PlanService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }




        #region --- Transfer Plan ---
        public async Task<jewelry.Model.Production.Plan.Transfer.Response> Transfer(jewelry.Model.Production.Plan.Transfer.Request  request)
        {
            ValidateRequest(request);

            var plans = await GetProductionPlans(request.Plans.Select(x => x.Id).ToArray());
            var response = new jewelry.Model.Production.Plan.Transfer.Response { Message = "success" };

            var transferData = await PrepareTransferData(request, plans);

            if (transferData.HasAnyValidPlans)
            {
                await ProcessTransfer(transferData);
            }

            response.Errors.AddRange(transferData.Errors);
            return response;
        }
        private void ValidateRequest(jewelry.Model.Production.Plan.Transfer.Request request)
        {
            if (request.FormerStatus == request.TargetStatus)
            {
                throw new HandleException(ErrorMessage.InvalidRequest);
            }
        }
        private async Task<List<TbtProductionPlan>> GetProductionPlans(int[] planIds)
        {
            var plans = await _jewelryContext.TbtProductionPlan
                .Include(x => x.TbtProductionPlanStatusHeader)
                .Where(item => planIds.Contains(item.Id))
                .ToListAsync();

            if (!plans.Any())
            {
                throw new HandleException(ErrorMessage.NotFound);
            }

            return plans;
        }
        private async Task<TransferData> PrepareTransferData(jewelry.Model.Production.Plan.Transfer.Request request, List<TbtProductionPlan> plans)
        {
            var data = new TransferData
            {
                DateNow = DateTime.UtcNow,
                Running = await _runningNumberService.GenerateRunningNumberForGold("PLT")
            };

            foreach (var planRequest in request.Plans)
            {
                var validationResult = ValidatePlanForTransfer(planRequest, plans, request);

                if (validationResult.IsValid)
                {
                    var targetPlan = plans.First(x => x.Id == planRequest.Id);
                    AddValidPlanData(data, targetPlan, request);
                }
                else
                {
                    data.Errors.Add(new TransferResponseItem
                    {
                        Id = planRequest.Id,
                        Wo = planRequest.Wo,
                        WoNumber = planRequest.WoNumber,
                        Message = validationResult.ErrorMessage
                    });
                }
            }

            return data;
        }
        private (bool IsValid, string ErrorMessage) ValidatePlanForTransfer(
            jewelry.Model.Production.Plan.Transfer.RequestItem planRequest,
            List<TbtProductionPlan> plans,
            jewelry.Model.Production.Plan.Transfer.Request request)
        {
            var targetPlan = plans.FirstOrDefault(x => x.Id == planRequest.Id);

            if (targetPlan == null)
                return (false, ErrorMessage.NotFound);

            if (targetPlan.Status == ProductionPlanStatus.Completed)
                return (false, ErrorMessage.PlanCompleted);

            if (request.TargetStatus == ProductionPlanStatus.Completed && targetPlan.Status != ProductionPlanStatus.Price)
                return (false, ErrorMessage.PlanNeedPrice);

            if (targetPlan.TbtProductionPlanStatusHeader.Any(x =>
                x.IsActive && x.Status == request.TargetStatus))
            {
                return (false, ErrorMessage.StatusAlready);
            }

            return (true, null);
        }
        private void AddValidPlanData(TransferData data, TbtProductionPlan plan, jewelry.Model.Production.Plan.Transfer.Request request)
        {
            var newStatus = CreateNewStatus(plan, request, data.DateNow);
            data.NewStatuses.Add(newStatus);

            var transferStatus = CreateTransferStatus(plan, request, data.Running, data.DateNow);
            data.TransferStatuses.Add(transferStatus);

            var currentStatus = (ProductionPlanStatusEnum)request.TargetStatus;
            plan.Status = currentStatus.GetWatingStatus();
            plan.UpdateDate = data.DateNow;
            plan.UpdateBy = request.TransferBy ?? _admin;
            data.UpdatePlans.Add(plan);
        }
        private TbtProductionPlanStatusHeader CreateNewStatus(
            TbtProductionPlan plan,
            jewelry.Model.Production.Plan.Transfer.Request request,
            DateTime dateNow)
        {
            return new TbtProductionPlanStatusHeader
            {
                CreateDate = dateNow,
                CreateBy = request.TransferBy ?? _admin,
                UpdateDate = dateNow,
                UpdateBy = request.TransferBy ?? _admin,
                IsActive = true,
                ProductionPlanId = plan.Id,
                Status = request.TargetStatus
            };
        }
        private TbtProductionPlanTransferStatus CreateTransferStatus(
            TbtProductionPlan plan,
            jewelry.Model.Production.Plan.Transfer.Request request,
            string running,
            DateTime dateNow)
        {
            return new TbtProductionPlanTransferStatus
            {
                Running = running,
                Wo = plan.Wo,
                WoNumber = plan.WoNumber,
                ProductionPlanId = plan.Id,
                CreateDate = dateNow,
                CreateBy = request.TransferBy ?? _admin,
                FormerStatus = request.FormerStatus,
                TargetStatus = request.TargetStatus
            };
        }
        private async Task ProcessTransfer(TransferData data)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            if (data.NewStatuses.Any())
            {
                await _jewelryContext.TbtProductionPlanStatusHeader.AddRangeAsync(data.NewStatuses);
                await _jewelryContext.SaveChangesAsync();

                // Link transfer statuses with new status headers
                foreach (var transfer in data.TransferStatuses)
                {
                    var match = data.NewStatuses.First(x => x.ProductionPlanId == transfer.ProductionPlanId);
                    transfer.TargetStatusId = match.Id;
                }
            }

            if (data.TransferStatuses.Any())
            {
                await _jewelryContext.TbtProductionPlanTransferStatus.AddRangeAsync(data.TransferStatuses);
                await _jewelryContext.SaveChangesAsync();
            }

            if (data.UpdatePlans.Any())
            {
                _jewelryContext.TbtProductionPlan.UpdateRange(data.UpdatePlans);
                await _jewelryContext.SaveChangesAsync();
            }

            scope.Complete();
        }
        private class TransferData
        {
            public DateTime DateNow { get; set; }
            public string Running { get; set; }
            public List<TbtProductionPlanStatusHeader> NewStatuses { get; } = new();
            public List<TbtProductionPlanTransferStatus> TransferStatuses { get; } = new();
            public List<TbtProductionPlan> UpdatePlans { get; } = new();
            public List<TransferResponseItem> Errors { get; } = new();
            public bool HasAnyValidPlans => NewStatuses.Any();
        }
        #endregion
    }
}
