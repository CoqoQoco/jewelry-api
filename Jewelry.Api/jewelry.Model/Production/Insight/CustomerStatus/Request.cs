using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.CustomerStatus
{
    public class Request
    {
        // รหัสลูกค้า (tbm_customer.code) หรือค่า customer_number ของใบงานที่ resolve แล้ว (ตัดช่องว่าง/ไม่สนตัวพิมพ์) — ไม่เกิน 10 รหัส
        public List<string> CustomerCodes { get; set; } = new List<string>();
    }
}
