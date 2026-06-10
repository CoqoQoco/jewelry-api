using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanStageImage
{
    public class PlanStageImageRequest
    {
        public int Id { get; set; }
        public string Stage { get; set; }
        public List<IFormFile> Images { get; set; }
    }
}
