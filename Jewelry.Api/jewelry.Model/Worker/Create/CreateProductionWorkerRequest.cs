using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Worker.Create
{
    public class CreateProductionWorkerRequest
    {
        public string Code { get; set; }

        public string NameTh { get; set; }
        public string? NameEn { get; set; }

        public int Type { get; set; }
    }
}
