namespace Jewelry.Service.Sale.ExportShipment;

public interface IExportShipmentService
{
    Task<jewelry.Model.Sale.ExportShipment.GenerateNumber.Response> GenerateNumber();
    Task<jewelry.Model.Sale.ExportShipment.Upsert.Response> Upsert(jewelry.Model.Sale.ExportShipment.Upsert.Request request);
    Task<jewelry.Model.Sale.ExportShipment.Get.Response> Get(string running);
    IQueryable<jewelry.Model.Sale.ExportShipment.List.Response> List(jewelry.Model.Sale.ExportShipment.List.Search? search);
    Task Delete(string running);
    Task<jewelry.Model.Sale.ExportShipment.AddItems.Response> AddItems(jewelry.Model.Sale.ExportShipment.AddItems.Request request);
    Task<jewelry.Model.Sale.ExportShipment.RemoveItems.Response> RemoveItems(jewelry.Model.Sale.ExportShipment.RemoveItems.Request request);
}
