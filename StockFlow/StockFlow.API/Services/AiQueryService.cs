using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockFlow.API.Dtos;
using StockFlow.API.Interfaces;
using StockFlow.API.Models;

namespace StockFlow.API.Services;

public class AiQueryService(
    IHttpClientFactory httpClientFactory,
    IItemRepository itemRepository,
    IConfiguration configuration) : IAiQueryService
{
    private const string SystemPrompt = """
        You translate warehouse inventory questions into a JSON filter.
        Respond with ONLY JSON matching this exact shape, nothing else:
        {"warehouseId": <integer or null>, "lowStockOnly": <true or false>}
        """;

    public async Task<QueryResponseDto> AskAsync(string question)
    {
        var intent = await GetIntentFromAiAsync(question);

        var items = await itemRepository.GetAllAsync(intent.WarehouseId, intent.LowStockOnly);

        return new QueryResponseDto(
            intent.WarehouseId,
            intent.LowStockOnly,
            items.Select(ToDto));
    }

    private async Task<ItemFilterIntent> GetIntentFromAiAsync(string question)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.openai.com/v1/");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", configuration["OpenAI:ApiKey"]);

        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = question }
            },
            response_format = new { type = "json_object" }
        };

        var response = await client.PostAsJsonAsync("chat/completions", requestBody);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"OpenAI request failed ({response.StatusCode}): {errorBody}");
        }

        var completion = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>();
        var rawJson = completion?.Choices.FirstOrDefault()?.Message.Content;

        if (string.IsNullOrWhiteSpace(rawJson))
            return new ItemFilterIntent(null, false);

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<ItemFilterIntent>(rawJson, options)
                   ?? new ItemFilterIntent(null, false);
        }
        catch (JsonException)
        {
            return new ItemFilterIntent(null, false);
        }
    }

    private static ItemDto ToDto(Item item) =>
        new(item.Id, item.Sku, item.Name, item.Quantity, item.ReorderThreshold, item.WarehouseId);

    private record ItemFilterIntent(int? WarehouseId, bool LowStockOnly);

    private record OpenAiChatResponse([property: JsonPropertyName("choices")] List<OpenAiChoice> Choices);

    private record OpenAiChoice([property: JsonPropertyName("message")] OpenAiMessage Message);

    private record OpenAiMessage([property: JsonPropertyName("content")] string Content);
}
