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
        public string? Code { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public int NextStatus { get; set; }
        public string NextStatusName { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }

        public string? CatagoryName { get; set; }
        public string? DesignBy { get; set; }

        public string ImgDesign { get; set; }
        public string ImgResin { get; set; }
        public string ImgCastingSilver { get; set; }
        public string ImgCasting { get; set; }
        public string ImgCutting { get; set; }
        public string ImgStore { get; set; }
    }
}
