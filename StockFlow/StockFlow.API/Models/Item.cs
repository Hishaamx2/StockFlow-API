namespace StockFlow.API.Models;

//Every item object will have these 6 labeled slots

public class Item
{
    public int Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public int Quantity { get; set; }
    public int ReorderThreshold { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}
