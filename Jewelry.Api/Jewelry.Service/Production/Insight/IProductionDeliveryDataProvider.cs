using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    public interface IProductionDeliveryDataProvider
    {
        // key = production_plan_id — มีเฉพาะแผนที่ "เสร็จ" แล้วเท่านั้น (transfer แรกเข้า status 100, หรือ
        // fallback completed_date ถ้าไม่มี row transfer — ดู FACTS ในสเปก) ไม่มี key = ยังไม่เสร็จ
        Task<Dictionary<int, DateTime>> GetDoneDatesAsync();

        // key = customer code (tbm_customer.code) -> tbm_customer.name_th
        Task<Dictionary<string, string>> GetCustomerNamesAsync();
    }
}
