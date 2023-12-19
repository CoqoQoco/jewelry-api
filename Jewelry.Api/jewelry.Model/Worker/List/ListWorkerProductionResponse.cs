using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Worker.List
{
    public class ListWorkerProductionResponse
    {
        public string Code { get; set; }

        public string NameTh { get; set; }
        public string? NameEn { get; set; }

        public int Type { get; set; }
        public string? TypeName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }
}
