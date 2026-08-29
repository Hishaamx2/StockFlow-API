namespace StockFlow.API.Dtos;

public record QueryRequestDto(string Question);

public record QueryResponseDto(int? WarehouseId, bool LowStockOnly, IEnumerable<ItemDto> Items);
