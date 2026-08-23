namespace StockFlow.API.Models;

//Defines Warehouse with its 3 slots and collection of Items
public class Warehouse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Location { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
