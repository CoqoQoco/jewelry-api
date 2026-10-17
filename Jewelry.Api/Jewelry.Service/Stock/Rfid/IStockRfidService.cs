namespace Jewelry.Service.Stock.Rfid;

public interface IStockRfidService
{
    Task<jewelry.Model.Stock.Rfid.Bind.Response> Bind(jewelry.Model.Stock.Rfid.Bind.Request request);

    Task<jewelry.Model.Stock.Rfid.Unbind.Response> Unbind(jewelry.Model.Stock.Rfid.Unbind.Request request);

    Task<jewelry.Model.Stock.Rfid.GetByEpc.Response> GetByEpc(jewelry.Model.Stock.Rfid.GetByEpc.Request request);

    Task<jewelry.Model.Stock.Rfid.GetByEpcs.Response> GetByEpcs(jewelry.Model.Stock.Rfid.GetByEpcs.Request request);

    Task<jewelry.Model.Stock.Rfid.GetByStock.Response> GetByStock(jewelry.Model.Stock.Rfid.GetByStock.Request request);
}
