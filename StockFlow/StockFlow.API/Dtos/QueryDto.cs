namespace StockFlow.API.Dtos;

public record ConversationTurn(string Question, string Answer);

public record QueryRequestDto(string Question, List<ConversationTurn>? History = null);

public record QueryResponseDto(string Answer);
