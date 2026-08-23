using Microsoft.AspNetCore.Mvc;
using StockFlow.API.Dtos;
using StockFlow.API.Interfaces;
using StockFlow.API.Models;

namespace StockFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController(IItemRepository itemRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetAll(
        [FromQuery] int? warehouseId,
        [FromQuery] bool lowStockOnly = false)
    {
        var items = await itemRepository.GetAllAsync(warehouseId, lowStockOnly);
        return Ok(items.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetById(int id)
    {
        var item = await itemRepository.GetByIdAsync(id);
        if (item is null)
            return NotFound();

        return Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(ItemWriteDto dto)
    {
        var item = new Item
        {
            Sku = dto.Sku,
            Name = dto.Name,
            Quantity = dto.Quantity,
            ReorderThreshold = dto.ReorderThreshold,
            WarehouseId = dto.WarehouseId
        };

        var created = await itemRepository.AddAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ItemWriteDto dto)
    {
        var item = new Item
        {
            Id = id,
            Sku = dto.Sku,
            Name = dto.Name,
            Quantity = dto.Quantity,
            ReorderThreshold = dto.ReorderThreshold,
            WarehouseId = dto.WarehouseId
        };

        var updated = await itemRepository.UpdateAsync(item);
        if (!updated)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await itemRepository.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    private static ItemDto ToDto(Item item) =>
        new(item.Id, item.Sku, item.Name, item.Quantity, item.ReorderThreshold, item.WarehouseId);
}
