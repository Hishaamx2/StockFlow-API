namespace StockFlow.API.Dtos;

public record ItemDto(int Id, string Sku, string Name, int Quantity, int ReorderThreshold, int WarehouseId);

public record ItemWriteDto(string Sku, string Name, int Quantity, int ReorderThreshold, int WarehouseId);

//defines what the database looks like internally so itdefine what the API is allowed to accept from and hand back to the outside world