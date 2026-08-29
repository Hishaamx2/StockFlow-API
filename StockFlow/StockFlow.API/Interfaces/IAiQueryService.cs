using StockFlow.API.Dtos;

namespace StockFlow.API.Interfaces;

public interface IAiQueryService
{
    Task<QueryResponseDto> AskAsync(string question);
}
