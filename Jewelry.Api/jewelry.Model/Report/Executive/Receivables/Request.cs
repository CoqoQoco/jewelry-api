using Kendo.DynamicLinqCore;

namespace jewelry.Model.Report.Executive.Receivables
{
    public class Request : DataSourceRequest
    {
        // shadow ฐาน DataSourceRequest.Filter (Kendo dynamic filter object) โดยตั้งใจ —
        // endpoint นี้ไม่ใช้ dynamic filter, ใช้ string enum ธรรมดาแทน (ตรวจแล้วว่า
        // Newtonsoft.Json CamelCasePropertyNamesContractResolver bind JSON key "filter"
        // เข้า property ที่ derived class ประกาศ ไม่ชนกับของฐาน)
        public new string? Filter { get; set; } = "unpaid";
    }
}
