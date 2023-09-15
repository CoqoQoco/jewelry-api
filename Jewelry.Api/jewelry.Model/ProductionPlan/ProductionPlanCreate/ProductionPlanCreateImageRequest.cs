using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanCreate
{
    public class ProductionPlanCreateImageRequest
    {
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public List<IFormFile> Images { get; set; }
    }
}
