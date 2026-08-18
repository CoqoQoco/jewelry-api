using jewelry.Model.Sale.ExportShipment.Common;

namespace jewelry.Model.Sale.ExportShipment.RemoveItems;

public class Response
{
    public List<ItemDto> Items { get; set; } = new();
}
