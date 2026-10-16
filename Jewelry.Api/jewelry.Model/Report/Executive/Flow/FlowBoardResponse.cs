using System;
using System.Collections.Generic;

namespace jewelry.Model.Report.Executive.Flow
{
    public class FlowBoardResponse
    {
        public DateTimeOffset AsOf { get; set; }
        public FlowSeriesData Flow { get; set; } = new FlowSeriesData();
        public FlowGoldLossData GoldLoss { get; set; } = new FlowGoldLossData();
        public FlowGoldNotReturnedData GoldNotReturned { get; set; } = new FlowGoldNotReturnedData();
        public FlowBoardSalesData Sales { get; set; } = new FlowBoardSalesData();
        public FlowMixData Mix { get; set; } = new FlowMixData();
        public List<FlowTrustItem> Trust { get; set; } = new List<FlowTrustItem>();
    }

    public class FlowSeriesData
    {
        // 12 เดือนปฏิทินเวลาไทย เก่าสุดก่อน (รวมเดือนปัจจุบันที่ยังไม่จบ)
        public List<FlowMonthData> Months { get; set; } = new List<FlowMonthData>();
        public int CreatedPcs12m { get; set; }
        public int CompletedPcs12m { get; set; }
        public int BacklogPcs { get; set; }
    }

    public class FlowMonthData
    {
        // yyyy-MM (เวลาไทย)
        public string Month { get; set; } = null!;
        public int CreatedPcs { get; set; }
        public int CompletedPcs { get; set; }
        public int NewStockPieces { get; set; }
        public int InvoiceCount { get; set; }

        // null = เดือนนั้นไม่มีบิล THB ที่มียอดรวม
        public decimal? InvoiceThb { get; set; }
    }

    public class FlowGoldLossData
    {
        public int WindowDays { get; set; }
        public decimal SendG { get; set; }
        public decimal ReceivedG { get; set; }
        public decimal LossG { get; set; }
        public decimal? LossPercent { get; set; }
        public decimal? GoldLossPercent { get; set; }
        public decimal? SilverLossPercent { get; set; }
        public decimal ExcessOverTargetG { get; set; }
        public int WorkersOverTargetCount { get; set; }
    }

    public class FlowGoldNotReturnedData
    {
        public decimal GoldG { get; set; }
        public decimal SilverG { get; set; }
        public decimal? QueueSharePercent { get; set; }
    }

    public class FlowCustomerShare
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int InvoiceCount { get; set; }
        public decimal Thb { get; set; }
        public decimal? SharePercent { get; set; }

        // WALKIN = ถังรวมลูกค้าหน้าร้าน ไม่ใช่ลูกค้ารายเดียว
        public bool IsBucket { get; set; }
    }

    public class FlowBoardSalesData
    {
        public decimal ThbTotalWithAmount { get; set; }

        // yyyy-MM-dd (เวลาไทย) ของบิล THB แรกที่มียอดรวม
        public string? FirstDateWithTotal { get; set; }
        public decimal CurrentMonthThb { get; set; }
        public decimal PreviousMonthThb { get; set; }
        public decimal? CurrentVsPreviousPercent { get; set; }
        public decimal? Top2CustomerSharePercent { get; set; }
        public List<FlowCustomerShare> TopCustomers { get; set; } = new List<FlowCustomerShare>();
        public int UsdInvoiceCount { get; set; }
        public bool UsdAmountsHeld { get; set; } = true;
        public bool PaymentHeld { get; set; } = true;
    }

    public class FlowMixData
    {
        public FlowMixPopulation Wip { get; set; } = new FlowMixPopulation();
        public FlowMixPopulation Stock { get; set; } = new FlowMixPopulation();
        public FlowMixPopulation Sold { get; set; } = new FlowMixPopulation();
    }

    public class FlowMixPopulation
    {
        public int TotalPcs { get; set; }

        // YG, WG, PG, SV, Unknown (คงลำดับนี้เสมอ)
        public List<FlowShareItem> Gold { get; set; } = new List<FlowShareItem>();
        // 18K, 9K, 14K, 10K, Silver, Unknown (คงลำดับนี้เสมอ)
        public List<FlowShareItem> Size { get; set; } = new List<FlowShareItem>();
        public List<FlowShareItem> ProductType { get; set; } = new List<FlowShareItem>();
    }
}
