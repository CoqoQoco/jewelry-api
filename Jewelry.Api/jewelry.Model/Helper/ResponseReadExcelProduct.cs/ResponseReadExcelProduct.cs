using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Helper.ResponseReadExcelProduct.cs
{
    public class ResponseReadExcelProduct
    {
        public string WO_Number { get; set; }
        public string WO { get; set; }

        public string ProductCode { get; set; }
        public string Mold { get; set; }

        public string Mat_1 { get; set; }
        public string Mat_2 { get; set; }
        public string Mat_3 { get; set; }

        public DateTime ReceivedDate { get; set; }
        public DateTime RequiredDate { get; set; }

        public string Customer { get; set; }
    }
}
