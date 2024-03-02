using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Worker.WorkerWages
{
    public class SearchWorkerWagesResponse
    {
        public DateTime WagesDateStart { get; set; }
        public DateTime WagesDateEnd { get; set; }
        public decimal? TotalGoldQtySend { get; set; }
        public decimal? TotalGoldWeightSend { get; set; }
        public decimal? TotalGoldQtyCheck { get; set; }
        public decimal? TotalGoldWeightCheck { get; set; }
        public decimal? TotalWages { get; set; }
        public List<SearchWorkerWages>? Items { get; set; }
        public SearchWorkerWagesResponse()
        {
            Items = new List<SearchWorkerWages>();
        }
    }
    public class SearchWorkerWages
    {
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public string WoText { get; set; }
        public string ProductNumber { get; set; }
        public string ProductName { get; set; }

        public int Status { get; set; }
        public string StatusName { get; set; }
        public string? StatusDescription { get; set; }

        public string? Gold { get; set; }

        public decimal? GoldQtySend { get; set; }
        public decimal? GoldWeightSend { get; set; }
        public decimal? GoldQtyCheck { get; set; }
        public decimal? GoldWeightCheck { get; set; }
        public string? Description { get; set; }

        public decimal? Wages { get; set; }
        public decimal? TotalWages { get; set; }
        public int WagesStatus { get; set; }

        public DateTime? JobDate { get; set; }
    }
}
