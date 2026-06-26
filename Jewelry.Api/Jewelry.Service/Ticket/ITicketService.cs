using jewelry.Model.Ticket;
using Kendo.DynamicLinqCore;
using System.Threading.Tasks;

namespace Jewelry.Service.Ticket;

public interface ITicketService
{
    Task<string> CreateTicket(CreateTicketRequest request);
    Task<DataSourceResult> SearchTicket(SearchTicketRequest request);
    Task<DataSourceResult> GetMyTickets(SearchTicketRequest request);
    Task<string> UpdateTicketStatus(UpdateTicketStatusRequest request);
    Task<string> UpdateTicketDev(UpdateTicketDevRequest request);
    Task<string> AddTicketLog(AddTicketLogRequest request);
    Task<string> AddTicketComment(AddTicketCommentRequest request);
    Task<string> AddMyTicketComment(AddMyTicketCommentRequest request);
    Task<string> DeleteTicketComment(DeleteTicketCommentRequest request);
    Task<string> DeleteMyTicketComment(DeleteTicketCommentRequest request);
    Task<int> CountOpen();
}
