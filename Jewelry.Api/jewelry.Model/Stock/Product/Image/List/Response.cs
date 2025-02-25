using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.Image.List
{
    public class Response
    {
        public int Id { get; set; }

        public string Name { get; set; } 
        public int Year { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } 
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public string? Remark { get; set; }
        public bool? IsActive { get; set; }

        public string NamePath { get; set; } 
    }
}
