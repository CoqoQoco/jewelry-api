using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanList
{
    public class PlanListReponse
    {
        public int Id { get; set; }
        public string MoldCode { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }

        public int Status { get; set; }
        public string StatusName { get; set; }

        public string Image { get; set; }
    }
}
