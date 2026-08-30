using Microsoft.EntityFrameworkCore;
using StockFlow.API.Data;
using StockFlow.API.Interfaces;
using StockFlow.API.Models;

//Does the requested HTTP task

namespace StockFlow.API.Repositories;

public class ItemRepository(AppDbContext context) : IItemRepository
{
    public async Task<IEnumerable<Item>> GetAllAsync(int? warehouseId = null, bool lowStockOnly = false, string? nameSearch = null)
    {
        var query = context.Items.AsQueryable();

        if (warehouseId is not null)
            query = query.Where(i => i.WarehouseId == warehouseId);

        if (lowStockOnly)
            query = query.Where(i => i.Quantity < i.ReorderThreshold);

        if (!string.IsNullOrWhiteSpace(nameSearch))
            query = query.Where(i =>
                EF.Functions.ILike(i.Name, $"%{nameSearch}%") ||
                EF.Functions.ILike(i.Sku, $"%{nameSearch}%"));

        return await query.ToListAsync();
    }

    public async Task<Item?> GetByIdAsync(int id) =>
        await context.Items.FindAsync(id);

    public async Task<Item> AddAsync(Item item)
    {
        context.Items.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task<bool> UpdateAsync(Item item)
    {
        var existing = await context.Items.FindAsync(item.Id);
        if (existing is null)
            return false;

        existing.Sku = item.Sku;
        existing.Name = item.Name;
        existing.Quantity = item.Quantity;
        existing.ReorderThreshold = item.ReorderThreshold;
        existing.WarehouseId = item.WarehouseId;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await context.Items.FindAsync(id);
        if (existing is null)
            return false;

        context.Items.Remove(existing);
        await context.SaveChangesAsync();
        return true;
    }
}
