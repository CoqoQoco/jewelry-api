using System.Collections.Generic;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;

namespace jewelry.Model.Ticket;

public class CreateTicketRequest
{
    public int Type { get; set; }
    public string TopicRoute { get; set; } = null!;
    public string TopicName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<IFormFile>? Images { get; set; }
}

public class SearchTicketRequest : DataSourceRequest
{
    public long? TicketId { get; set; }
    public int? Status { get; set; }
    public int? Type { get; set; }
    public string? TopicRoute { get; set; }
    public string? Keyword { get; set; }
}

public class UpdateTicketStatusRequest
{
    public long TicketId { get; set; }
    public int Status { get; set; }
}

public class UpdateTicketDevRequest
{
    public long TicketId { get; set; }
    public string? DevAnalysis { get; set; }
    public string? DevResponse { get; set; }
}

public class AddTicketLogRequest { public long TicketId { get; set; } public string Detail { get; set; } = null!; }
