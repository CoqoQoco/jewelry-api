using jewelry.Model.Production.PrePlan;
using Jewelry.Data.Models.Jewelry;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.PrePlan;

public interface IProductionPrePlanService
{
    Task<IList<SearchPrePlanResponse>> Search(SearchPrePlanRequest request);
    Task<GetPrePlanResponse> Get(int id);
    Task<string> Create(CreatePrePlanRequest request);
    Task<string> Update(int id, UpdatePrePlanRequest request);
    Task<string> Submit(int id);
    Task<string> Approve(int id, ApprovePrePlanRequest request);
    Task<string> Reject(int id, RejectPrePlanRequest request);
    Task<List<AvailableForPlanResponse>> GetAvailableForPlan(string? moldCode);
    Task LinkProductionPlan(int prePlanItemId, TbtProductionPlan plan);
    Task<string> UploadApproveDocument(IFormFile file);
}
