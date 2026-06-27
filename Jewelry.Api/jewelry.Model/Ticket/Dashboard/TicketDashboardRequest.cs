using System;

namespace jewelry.Model.Ticket.Dashboard
{
    public class TicketDashboardRequest
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }
}
