using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Mold.CheckOutList
{
    public class CheckOutListResponse
    {
        public int Id { get; set; }

        public string Running { get; set; }

        public string Mold { get; set; }


        public DateTime CreateDate { get; set; }

        public string CreateBy { get; set; } 

        public DateTime? UpdateDate { get; set; }

        public string? UpdateBy { get; set; }


        public DateTime CheckOutDate { get; set; }

        public string CheckOutName { get; set; }

        public string? CheckOutDescription { get; set; }

        public DateTime ReturnDateSet { get; set; }
        public DateTime ReturnDateSetLocal { get; set; }


        public string? ReturnName { get; set; }
        public string? ReturnDescription { get; set; }

        public bool IsOverReturn { get; set; }
        public bool IsSetReturn { get; set; }

        public string Image { get; set; }
        public string Category { get; set; }
        public string  CategoryCode { get; set;}
    }
}
    