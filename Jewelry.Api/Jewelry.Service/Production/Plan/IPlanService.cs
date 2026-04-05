using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Plan
{
    public interface IPlanService
    {
        IQueryable<jewelry.Model.Production.Plan.StatusDetailList.Response> StatusDetailList(jewelry.Model.Production.Plan.StatusDetailList.RequestSearch request);
        IQueryable<jewelry.Model.Production.Plan.TransferList.Response> TransferList(jewelry.Model.Production.Plan.TransferList.RequestSearch request);
        Task<jewelry.Model.Production.Plan.Transfer.Response> Transfer(jewelry.Model.Production.Plan.Transfer.Request request);

        Task<jewelry.Model.Production.Plan.DailyPlan.Response> GetDailyReport(jewelry.Model.Production.Plan.DailyPlan.Criteria request);
        Task<jewelry.Model.Production.Plan.MonthlyReport.Response> GetPlanSuccessMonthlyReport(jewelry.Model.Production.Plan.MonthlyReport.Criteria request);
        IQueryable<jewelry.Model.Production.Plan.ListComplete.Response> PlanCompleted(jewelry.Model.Production.Plan.ListComplete.Search request);

        Task<jewelry.Model.Production.Plan.GoldLossMonthlyReport.SearchResponse> GetGoldLossMonthlyReport(jewelry.Model.Production.Plan.GoldLossMonthlyReport.SearchRequest request);
        Task<string> SaveGoldLossMonthlyReport(jewelry.Model.Production.Plan.GoldLossMonthlyReport.SaveRequest request);

        Task<jewelry.Model.Production.Plan.GoldLossTangReport.SearchResponse> GetGoldLossTangReport(jewelry.Model.Production.Plan.GoldLossTangReport.SearchRequest request);
        Task<string> SaveGoldLossTangReport(jewelry.Model.Production.Plan.GoldLossTangReport.SaveRequest request);
        Task<string> CreateGoldLossJob(jewelry.Model.Production.Plan.GoldLossTangReport.CreateJobRequest request);
        Task<List<jewelry.Model.Production.Plan.GoldLossTangReport.JobListRow>> GetGoldLossJobList(jewelry.Model.Production.Plan.GoldLossTangReport.JobListRequest request);
        Task<jewelry.Model.Production.Plan.GoldLossTangReport.JobDetailResponse> GetGoldLossJobById(int jobId);
        Task<string> UpdateGoldLossJob(jewelry.Model.Production.Plan.GoldLossTangReport.UpdateJobRequest request);
    }
}
