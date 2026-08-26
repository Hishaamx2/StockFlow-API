using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.API.Dtos;
using StockFlow.API.Interfaces;
using StockFlow.API.Models;

namespace StockFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehousesController(IWarehouseRepository warehouseRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetAll()
    {
        var warehouses = await warehouseRepository.GetAllAsync();
        return Ok(warehouses.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WarehouseDto>> GetById(int id)
    {
        var warehouse = await warehouseRepository.GetByIdAsync(id);
        if (warehouse is null)
            return NotFound();

        return Ok(ToDto(warehouse));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<WarehouseDto>> Create(WarehouseWriteDto dto)
    {
        var warehouse = new Warehouse
        {
            Name = dto.Name,
            Location = dto.Location
        };

        var created = await warehouseRepository.AddAsync(warehouse);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, WarehouseWriteDto dto)
    {
        var warehouse = new Warehouse
        {
            Id = id,
            Name = dto.Name,
            Location = dto.Location
        };

        var updated = await warehouseRepository.UpdateAsync(warehouse);
        if (!updated)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await warehouseRepository.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    private static WarehouseDto ToDto(Warehouse warehouse) =>
        new(warehouse.Id, warehouse.Name, warehouse.Location);
}
