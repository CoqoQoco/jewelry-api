using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    public interface IProductionMaterialGemDataProvider
    {
        // โหลด tbt_stock_gem ทั้งตาราง (เล็กพอโหลดครั้งเดียวได้) — ใช้จับคู่ requirement ↔ stock ในหน่วยความจำ
        // ตามสั่ง (set-based, ไม่ query ซ้ำต่อ requirement line) — cache 5 นาที
        Task<List<ProductionMaterialGemEvaluator.StockGemRow>> GetStockGemsAsync();

        // tbm_gem.code (upper) -> name_en — ใช้ขึ้นต้น group_name ของ stock (ILIKE name_en||'%') — cache 5 นาที
        Task<Dictionary<string, string>> GetGemNamesAsync();
    }
}
