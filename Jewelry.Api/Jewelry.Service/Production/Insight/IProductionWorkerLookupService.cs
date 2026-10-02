using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    public interface IProductionWorkerLookupService
    {
        // tbm_worker.code -> name_th — ใช้แยก "ช่างจริง" กับ "รหัสคิวรองาน" (name_th ขึ้นต้นด้วย "รอ" เช่น
        // CG9K="รอจ่ายขัดชุบ 9K") — cache 10 นาที
        Task<Dictionary<string, string>> GetWorkerNamesAsync();

        // tbm_worker.code -> employment_type ('IN_HOUSE'|'OUTSIDE'|'SHOP'|null) — cache 10 นาที
        Task<Dictionary<string, string?>> GetWorkerEmploymentTypesAsync();
    }
}
