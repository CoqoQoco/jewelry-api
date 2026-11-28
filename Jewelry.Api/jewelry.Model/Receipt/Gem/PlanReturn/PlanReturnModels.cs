using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;

namespace jewelry.Model.Receipt.Gem.PlanReturn
{
    public class ReturnFromPlanRequest
    {
        public int ProductionPlanId { get; set; }
        public string ItemNo { get; set; } = null!;
        public string Mode { get; set; } = null!;
        public decimal ReturnQty { get; set; }
        public decimal ReturnWeight { get; set; }
        public long? InboundTransectionId { get; set; }
        public string Reason { get; set; } = null!;
        public string? Remark { get; set; }
        public DateTimeOffset RequestDate { get; set; }
        public decimal? ExpectedQty { get; set; }
        public decimal? ExpectedWeight { get; set; }
    }

    public class ReturnFromPlanResponse
    {
        public string? InboundRunning { get; set; }
        public bool IsFullReturn { get; set; }
    }

    public class ListLinkableInboundRequest
    {
        public string GemCode { get; set; } = null!;
        public int? ProductionPlanId { get; set; }
    }

    public class InboundLineResponse
    {
        public long TransectionId { get; set; }
        public string Running { get; set; } = null!;
        public DateTime RequestDate { get; set; }
        public string Code { get; set; } = null!;
        public decimal Qty { get; set; }
        public decimal QtyWeight { get; set; }
        public decimal RemainQty { get; set; }
        public decimal RemainWeight { get; set; }
        public string? JobOrPo { get; set; }
        public string? Remark1 { get; set; }
        public string? Remark2 { get; set; }
        public bool MatchesPlan { get; set; }
    }

    public class CheckDuplicateOutboundRequest
    {
        public List<CheckDuplicateOutboundItem> Items { get; set; } = new();
    }

    public class CheckDuplicateOutboundItem
    {
        public string Code { get; set; } = null!;
        public string Wo { get; set; } = null!;
        public int WoNumber { get; set; }
    }

    public class CheckDuplicateOutboundResponse
    {
        public string Code { get; set; } = null!;
        public string Wo { get; set; } = null!;
        public int WoNumber { get; set; }
        public string? OutboundRunning { get; set; }
        public DateTime? OutboundDate { get; set; }
        public decimal Qty { get; set; }
        public decimal Weight { get; set; }
    }

    public class ListUnlinkedReturnRequest : DataSourceRequest
    {
        public ListUnlinkedReturnSearch? Search { get; set; }
    }

    public class ListUnlinkedReturnSearch
    {
        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }
    }

    public class UnlinkedReturnResponse
    {
        public long TransectionId { get; set; }
        public string Running { get; set; } = null!;
        public DateTime RequestDate { get; set; }
        public string Code { get; set; } = null!;
        public decimal Qty { get; set; }
        public decimal QtyWeight { get; set; }
        public decimal RemainQty { get; set; }
        public decimal RemainWeight { get; set; }
        public string? JobOrPo { get; set; }
        public string? Remark1 { get; set; }
        public string? Remark2 { get; set; }

        public int? ProductionPlanId { get; set; }
        public string? WoText { get; set; }
        public string? SuggestedItemNo { get; set; }
        public string? SuggestedOutboundRunning { get; set; }
        public decimal? SuggestedQty { get; set; }
        public decimal? SuggestedWeight { get; set; }
    }
}
