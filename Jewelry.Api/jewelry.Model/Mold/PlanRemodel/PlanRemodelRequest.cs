using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanRemodel
{
    public class PlanRemodelRequest
    {
        public string Code { get; set; }
        public List<IFormFile> Images { get; set; }

        public string Catagory { get; set; }
        public string CatagoryCode { get; set; }
        public string WorkBy { get; set; }

        public string? Remark { get; set; }
        public string? RemodelBy { get; set; }
        public string Location { get; set; }
    }
}
