using Microsoft.EntityFrameworkCore;
using StockFlow.API.Data;
using StockFlow.API.Interfaces;
using StockFlow.API.Models;

namespace StockFlow.API.Repositories;

public class WarehouseRepository(AppDbContext context) : IWarehouseRepository
{
    public async Task<IEnumerable<Warehouse>> GetAllAsync() =>
        await context.Warehouses.ToListAsync();

    public async Task<Warehouse?> GetByIdAsync(int id) =>
        await context.Warehouses.FindAsync(id);

    public async Task<Warehouse> AddAsync(Warehouse warehouse)
    {
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();
        return warehouse;
    }

    public async Task<bool> UpdateAsync(Warehouse warehouse)
    {
        var existing = await context.Warehouses.FindAsync(warehouse.Id);
        if (existing is null)
            return false;

        existing.Name = warehouse.Name;
        existing.Location = warehouse.Location;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await context.Warehouses.FindAsync(id);
        if (existing is null)
            return false;

        context.Warehouses.Remove(existing);
        await context.SaveChangesAsync();
        return true;
    }
}
