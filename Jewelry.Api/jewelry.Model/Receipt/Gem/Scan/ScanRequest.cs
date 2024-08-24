using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Gem.Scan
{
    public class ScanRequest
    {
        public string ScanType { get; set; } //  SINGLE/LIST
        public IEnumerable<Scan> Scans { get; set; } = new List<Scan>();
    }
    public class Scan
    {
        public string Code { get; set; }
    }
}
