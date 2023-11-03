using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Master
{
    public class DeleteMasterModelRequest
    {
        public string Type { get; set; }
        public int Id { get; set; }
        public string Code { get; set; }
    }
}
