using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Master
{
    public class MasterModel
    {
        public int Id { get; set; }
        public string NameEn { get; set; }
        public string NameTh { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }


        public string? Prefix { get; set; }

        //zill
        public string? GoldCode { get; set; }
        public string? GoldNameTH { get; set; }
        public string? GoldNameEN { get; set; }

        public string? GoldSizeCode { get; set; }
        public string? GoldSizeNameTH { get; set; }
        public string? GoldSizeNameEN { get; set; }
    }
}
