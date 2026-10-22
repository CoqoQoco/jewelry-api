using System;
using System.Collections.Generic;

namespace jewelry.Model.Report.Executive.Digest
{
    public class Response
    {
        public DateTime AsOf { get; set; }
        public string Metal { get; set; } = "ALL";
        public List<DomainStatus> Domains { get; set; } = new List<DomainStatus>();
        public List<TopItem> Top { get; set; } = new List<TopItem>();
    }

    public class DomainStatus
    {
        // 'production' | 'sales' | 'material' | 'stock'
        public string Key { get; set; } = null!;

        // 'critical' | 'warning' | 'ok' | 'unknown' (unknown = available false)
        public string Status { get; set; } = "unknown";

        public int ProblemCount { get; set; }
        public int ForecastCount { get; set; }
        public bool Available { get; set; }
    }

    public class TopItem
    {
        public string Domain { get; set; } = null!;
        public string Severity { get; set; } = null!;

        // tab ย่อยสำหรับ click-through: production = wip|delivery|capacity|workers|gold|materials, domain อื่น = ชื่อ domain
        public string Tab { get; set; } = null!;

        // code / params / reportRef เป็นของ finding ต้นทางตรงๆ — UI แปลด้วย i18nPrefix ของแต่ละ domain
        public string Code { get; set; } = null!;
        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();
        public string ReportRef { get; set; } = null!;
    }
}
