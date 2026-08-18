using jewelry.Model.Sale.ExportShipment.Common;

namespace jewelry.Model.Sale.ExportShipment.AddItems;

public class Response
{
    public int Added { get; set; }
    public int Skipped { get; set; }
    public List<ItemDto> Items { get; set; } = new();
}
