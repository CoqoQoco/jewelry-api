using jewelry.Model.Exceptions;
using Jewelry.Data.Models.Jewelry;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jewelry.Service.Receipt.Production
{
    public static class ReceiptProductionServiceExtention
    {

        public static string MapToTbtStockProductReceiptPlanJson(this jewelry.Model.Receipt.Production.Draft.Create.Request request)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(request.Stocks, options);
        }

        public static List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock> GetStocksFromJsonDraft(this TbtStockProductReceiptPlan plan)
        {
            if (string.IsNullOrEmpty(plan.JsonDraft))
                return new List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock>();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                return JsonSerializer.Deserialize<List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock>>(plan.JsonDraft, options) ?? new List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock>();
            }
            catch(Exception ex)
            {
                throw new HandleException($"ไม่สามารถแปลงข้อมูล Draft ได้ เนื่องจากรูปแบบข้อมูลไม่ถูกต้อง กรุณาตรวจสอบข้อมูลหรือลองใหม่อีกครั้ง");
            }
        }
    }
}
