using System;
using System.Collections.Generic;

namespace jewelry.Model.Ticket.Dashboard
{
    public class TicketDashboardResponse
    {
        public TicketSummary Summary { get; set; } = new TicketSummary();
        public List<TicketStatusBreakdown> ByStatus { get; set; } = new List<TicketStatusBreakdown>();
        public List<TicketTopicBreakdown> ByTopic { get; set; } = new List<TicketTopicBreakdown>();
        public List<TicketTrend> Trend { get; set; } = new List<TicketTrend>();
        public TicketAging Aging { get; set; } = new TicketAging();
    }

    public class TicketSummary
    {
        public int Total { get; set; }
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int Closed { get; set; }
        public int Cancelled { get; set; }
        public int Bug { get; set; }
        public int Feature { get; set; }
        public int Unanalyzed { get; set; }
        public decimal ResolvedRate { get; set; }
    }

    public class TicketStatusBreakdown
    {
        public int StatusId { get; set; }
        public string NameTh { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TicketTopicBreakdown
    {
        public string TopicRoute { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TicketTrend
    {
        public DateTime Date { get; set; }
        public int Created { get; set; }
        public int Resolved { get; set; }
    }

    public class TicketAging
    {
        public int Today { get; set; }
        public int Days1To3 { get; set; }
        public int Days4To7 { get; set; }
        public int Over7 { get; set; }
    }
}
