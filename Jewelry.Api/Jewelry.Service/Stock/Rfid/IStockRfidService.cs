namespace Jewelry.Service.Stock.Rfid;

public interface IStockRfidService
{
    Task<jewelry.Model.Stock.Rfid.Bind.Response> Bind(jewelry.Model.Stock.Rfid.Bind.Request request);

    Task<jewelry.Model.Stock.Rfid.Unbind.Response> Unbind(jewelry.Model.Stock.Rfid.Unbind.Request request);

    Task<jewelry.Model.Stock.Rfid.GetByEpc.Response> GetByEpc(jewelry.Model.Stock.Rfid.GetByEpc.Request request);

    Task<jewelry.Model.Stock.Rfid.GetByEpcs.Response> GetByEpcs(jewelry.Model.Stock.Rfid.GetByEpcs.Request request);

    Task<jewelry.Model.Stock.Rfid.GetByStock.Response> GetByStock(jewelry.Model.Stock.Rfid.GetByStock.Request request);

    Task<jewelry.Model.Stock.Rfid.ListReceipts.Response> ListReceipts(jewelry.Model.Stock.Rfid.ListReceipts.Request request);

    Task<jewelry.Model.Stock.Rfid.ListByReceipt.Response> ListByReceipt(jewelry.Model.Stock.Rfid.ListByReceipt.Request request);

    Task<jewelry.Model.Stock.Rfid.ListUnbound.Response> ListUnbound(jewelry.Model.Stock.Rfid.ListUnbound.Request request);

    Task<jewelry.Model.Stock.Rfid.ListRecent.Response> ListRecent(jewelry.Model.Stock.Rfid.ListRecent.Request request);
}
