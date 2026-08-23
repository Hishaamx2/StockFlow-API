namespace StockFlow.API.Dtos;

public record WarehouseDto(int Id, string Name, string Location);

public record WarehouseWriteDto(string Name, string Location);
