using StockFlow.API.Models;

namespace StockFlow.API.Interfaces;

//This is the checlist of operations the repository must provide
public interface IWarehouseRepository
{
    Task<IEnumerable<Warehouse>> GetAllAsync();
    Task<Warehouse?> GetByIdAsync(int id);
    Task<Warehouse> AddAsync(Warehouse warehouse);
    Task<bool> UpdateAsync(Warehouse warehouse);
    Task<bool> DeleteAsync(int id);
}
