using Microsoft.EntityFrameworkCore;
using StockFlow.API.Models;

namespace StockFlow.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Warehouses.AnyAsync())
            return;

        var riverside = new Warehouse { Name = "Riverside DC", Location = "Riverside, CA" };
        var austin = new Warehouse { Name = "Austin Fulfillment Center", Location = "Austin, TX" };
        var newark = new Warehouse { Name = "Newark Hub", Location = "Newark, NJ" };

        context.Warehouses.AddRange(riverside, austin, newark);
        await context.SaveChangesAsync();

        context.Items.AddRange(
            new Item { Sku = "SKU-1001", Name = "USB-C Cable", Quantity = 5, ReorderThreshold = 20, WarehouseId = riverside.Id },
            new Item { Sku = "SKU-1002", Name = "Wireless Mouse", Quantity = 45, ReorderThreshold = 15, WarehouseId = riverside.Id },
            new Item { Sku = "SKU-1003", Name = "27-inch Monitor", Quantity = 8, ReorderThreshold = 10, WarehouseId = riverside.Id },
            new Item { Sku = "SKU-2001", Name = "Mechanical Keyboard", Quantity = 30, ReorderThreshold = 10, WarehouseId = austin.Id },
            new Item { Sku = "SKU-2002", Name = "Webcam", Quantity = 3, ReorderThreshold = 12, WarehouseId = austin.Id },
            new Item { Sku = "SKU-2003", Name = "Laptop Stand", Quantity = 60, ReorderThreshold = 20, WarehouseId = austin.Id },
            new Item { Sku = "SKU-3001", Name = "HDMI Cable", Quantity = 4, ReorderThreshold = 15, WarehouseId = newark.Id },
            new Item { Sku = "SKU-3002", Name = "Desk Lamp", Quantity = 22, ReorderThreshold = 8, WarehouseId = newark.Id }
        );

        await context.SaveChangesAsync();
    }
}
