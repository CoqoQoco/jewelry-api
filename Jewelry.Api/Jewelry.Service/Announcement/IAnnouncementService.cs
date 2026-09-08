using jewelry.Model.Announcement;
using Kendo.DynamicLinqCore;
using System.Threading.Tasks;

namespace Jewelry.Service.Announcement;

public interface IAnnouncementService
{
    Task<FeedAnnouncementResponse> Feed(FeedAnnouncementRequest request);
    Task<AnnouncementItemResponse> Get(GetAnnouncementRequest request);
    Task<DataSourceResult> Search(SearchAnnouncementRequest request);
    Task<CreateAnnouncementResponse> Create(CreateAnnouncementRequest request);
    Task<string> Update(UpdateAnnouncementRequest request);
    Task<string> TogglePublish(TogglePublishAnnouncementRequest request);
    Task<string> Delete(DeleteAnnouncementRequest request);
}
