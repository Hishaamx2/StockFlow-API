using StockFlow.API.Models;

namespace StockFlow.API.Interfaces;

public interface IItemRepository
{
    Task<IEnumerable<Item>> GetAllAsync(int? warehouseId = null, bool lowStockOnly = false, string? nameSearch = null);
    Task<Item?> GetByIdAsync(int id);
    Task<Item> AddAsync(Item item);
    Task<bool> UpdateAsync(Item item);
    Task<bool> DeleteAsync(int id);
}
