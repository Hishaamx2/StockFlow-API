using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockFlow.API.Dtos;
using StockFlow.API.Interfaces;

namespace StockFlow.API.Services;

public class AiQueryService(
    IHttpClientFactory httpClientFactory,
    IItemRepository itemRepository,
    IWarehouseRepository warehouseRepository,
    IConfiguration configuration) : IAiQueryService
{
    private static readonly HashSet<string> AllowedActions =
    [
        "list_items", "count_items", "total_quantity", "count_warehouses",
        "highest_stock_items", "compare_warehouses", "average_quantity", "out_of_scope"
    ];

    private const int HighestStockLimit = 5;

    private const string IntentSystemPrompt = """
        You translate warehouse inventory questions into a JSON intent.
        This system manages warehouses and items. Each item has a name, SKU, quantity, reorder threshold, and belongs to one warehouse.

        Respond with ONLY JSON matching this exact shape, nothing else:
        {"action": "list_items" | "count_items" | "total_quantity" | "count_warehouses" | "highest_stock_items" | "compare_warehouses" | "average_quantity" | "out_of_scope", "warehouseName": <string or null>, "lowStockOnly": <true or false>, "itemNameSearch": <string or null>}

        Guidance:
        - list_items: user wants to see items, optionally filtered by warehouse name, low stock, or item name/SKU search
        - count_items: user wants a count of how many distinct items/products match a filter (not asking about a specific named product's stock)
        - total_quantity: user wants a total unit count. This covers two cases: (1) "how many X do I have" for a specific named product (units in stock, not number of product listings), and "is X in stock" / "do we have any X" questions; (2) with NO product named, words like "inventory", "stock", or "units" (as opposed to "items" or "products") mean the sum of every unit across all items, e.g. "total inventory count", "how much stock do we have", "total units in stock"
        - count_warehouses: user wants to know how many warehouses exist
        - highest_stock_items: user wants to see items with the most/highest quantity in stock ("what's high in stock", "what do we have the most of", "best stocked items", "what's overstocked"), optionally scoped to a warehouse
        - compare_warehouses: user wants to compare warehouses against each other by stock level ("which warehouse has the most/least stock", "which warehouse is best/worst stocked", "compare warehouse stock")
        - average_quantity: user wants the average quantity across matching items ("what's the average stock level", "on average how much do we have of each item")
        - out_of_scope: the question is not about warehouses or items at all (weather, general chit-chat, pricing/cost data this system doesn't track, anything unrelated)

        Key distinction: if a specific product name is mentioned, "how many do I have" means total units (total_quantity), not a count of product listings.
        Questions naming a specific product (e.g. "how many USB-C cables do I have") are IN SCOPE, never out_of_scope.

        Key distinction: a question about ONE item's stock level (even "is it high" or "do we have a lot") is total_quantity or list_items with itemNameSearch,
        not highest_stock_items. highest_stock_items is for "what items in general are high/best stocked" with no specific item named.

        Key distinction: compare_warehouses is for questions comparing warehouses AGAINST EACH OTHER. A question about ONE named
        warehouse's contents (e.g. "what's in Riverside DC") is list_items with warehouseName set, not compare_warehouses.

        Only fill in warehouseName or itemNameSearch if the question actually references one, otherwise leave them null.

        "What are my X items" style questions that explicitly say "items" are self-contained list_items requests.

        This prompt may include real earlier user/assistant messages from this conversation, before the latest question.
        Rule for bare pronoun questions like "what are they", "which of those", "how many of them":
        - If ANY earlier user/assistant messages exist above the latest question, they were about items or
          warehouses (that is the only topic this system discusses). Treat the pronoun as referring to whatever
          was just discussed and answer with list_items (or the appropriate action), using the same filters
          implied by the earlier messages. Do not classify this as out_of_scope when earlier messages exist.
        - Only classify a bare pronoun question as out_of_scope when it is the very first message with no
          earlier messages above it at all.

        Examples:
        "how many USB-C cables do I have" -> {"action": "total_quantity", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": "USB-C"}
        "do we have any mice in stock" -> {"action": "total_quantity", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": "mice"}
        "how many different items do you have" -> {"action": "count_items", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}
        "what is the total inventory count" -> {"action": "total_quantity", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}
        "how much total stock do we have" -> {"action": "total_quantity", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}
        "what is in warehouse Riverside DC" -> {"action": "list_items", "warehouseName": "Riverside DC", "lowStockOnly": false, "itemNameSearch": null}
        "which items are running low" -> {"action": "list_items", "warehouseName": null, "lowStockOnly": true, "itemNameSearch": null}
        "what needs to be reordered" -> {"action": "list_items", "warehouseName": null, "lowStockOnly": true, "itemNameSearch": null}
        "what are my 8 items" -> {"action": "list_items", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}
        "how many warehouses do you have" -> {"action": "count_warehouses", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}
        "what's high in stock" -> {"action": "highest_stock_items", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}
        "what do we have the most of in Riverside DC" -> {"action": "highest_stock_items", "warehouseName": "Riverside DC", "lowStockOnly": false, "itemNameSearch": null}
        "which warehouse has the lowest stock" -> {"action": "compare_warehouses", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}
        "which warehouse is best stocked" -> {"action": "compare_warehouses", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}
        "what's the average quantity per item" -> {"action": "average_quantity", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}
        "how much does this cost" -> {"action": "out_of_scope", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}
        "what's the weather today" -> {"action": "out_of_scope", "warehouseName": null, "lowStockOnly": false, "itemNameSearch": null}

        Earlier turns in this conversation may be included before the latest question. Use them only to resolve
        references like "they", "those", "it", or "the same warehouse" when the latest question depends on them.
        """;

    private const string AnswerSystemPrompt = """
        You are StockBot, a warehouse inventory assistant.
        Using ONLY the data provided, answer the user's question naturally and concisely, in one sentence.
        Do not invent any information that is not present in the provided data.
        If a count or quantity in the data is zero because nothing matched a search, say you couldn't find a
        matching item rather than flatly stating "you have zero" — for example prefer "I couldn't find anything
        matching that" over "you have 0 of that".
        Do not use markdown formatting (no asterisks, no numbered or bulleted lists, no headers). Write a plain sentence only.
        """;

    private const int MaxHistoryTurns = 5;

    public async Task<QueryResponseDto> AskAsync(string question, List<ConversationTurn>? history)
    {
        var intent = await GetIntentAsync(question, history);

        if (intent.Action is null || !AllowedActions.Contains(intent.Action) || intent.Action == "out_of_scope")
        {
            return new QueryResponseDto(
                "I can only answer questions about your items and warehouses. Try asking about stock levels, a specific item, or a specific warehouse.");
        }

        var warehouseId = await ResolveWarehouseIdAsync(intent.WarehouseName);

        if (intent.Action == "list_items")
        {
            var items = await itemRepository.GetAllAsync(warehouseId, intent.LowStockOnly, intent.ItemNameSearch);
            return new QueryResponseDto(BuildItemListAnswer(items, intent.ItemNameSearch, "Found"));
        }

        if (intent.Action == "highest_stock_items")
        {
            var items = await itemRepository.GetAllAsync(warehouseId, false, intent.ItemNameSearch);
            var topItems = items.OrderByDescending(i => i.Quantity).Take(HighestStockLimit);
            return new QueryResponseDto(BuildItemListAnswer(topItems, intent.ItemNameSearch, "Highest-stock items"));
        }

        var resultData = await ExecuteAggregateAsync(intent, warehouseId);
        var answer = await PhraseAnswerAsync(question, resultData);

        return new QueryResponseDto(answer);
    }

    private static string BuildItemListAnswer(IEnumerable<Models.Item> items, string? searchTerm, string label)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            return string.IsNullOrWhiteSpace(searchTerm)
                ? "No items matched that."
                : $"I couldn't find anything matching \"{searchTerm}\".";
        }

        var descriptions = itemList.Select(i => $"{i.Name} (qty {i.Quantity})");
        var noun = itemList.Count == 1 ? "item" : "items";
        return $"{label} ({itemList.Count} {noun}): {string.Join(", ", descriptions)}.";
    }

    private async Task<QueryIntent> GetIntentAsync(string question, List<ConversationTurn>? history)
    {
        var messages = new List<object> { new { role = "system", content = IntentSystemPrompt } };

        if (history is not null)
        {
            foreach (var turn in history.TakeLast(MaxHistoryTurns))
            {
                messages.Add(new { role = "user", content = turn.Question });
                messages.Add(new { role = "assistant", content = turn.Answer });
            }
        }

        messages.Add(new { role = "user", content = question });

        var rawJson = await CallOpenAiAsync(messages, jsonMode: true);

        if (string.IsNullOrWhiteSpace(rawJson))
            return new QueryIntent("out_of_scope", null, false, null);

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<QueryIntent>(rawJson, options)
                   ?? new QueryIntent("out_of_scope", null, false, null);
        }
        catch (JsonException)
        {
            return new QueryIntent("out_of_scope", null, false, null);
        }
    }

    private async Task<int?> ResolveWarehouseIdAsync(string? warehouseName)
    {
        if (string.IsNullOrWhiteSpace(warehouseName))
            return null;

        var warehouses = await warehouseRepository.GetAllAsync();
        var match = warehouses.FirstOrDefault(w =>
            w.Name.Contains(warehouseName, StringComparison.OrdinalIgnoreCase));

        return match?.Id;
    }

    private async Task<object> ExecuteAggregateAsync(QueryIntent intent, int? warehouseId)
    {
        switch (intent.Action)
        {
            case "count_warehouses":
                var warehouses = await warehouseRepository.GetAllAsync();
                return new { warehouseCount = warehouses.Count() };

            case "count_items":
                var itemsForCount = await itemRepository.GetAllAsync(warehouseId, intent.LowStockOnly, intent.ItemNameSearch);
                return new { itemCount = itemsForCount.Count() };

            case "average_quantity":
                var itemsForAvg = await itemRepository.GetAllAsync(warehouseId, intent.LowStockOnly, intent.ItemNameSearch);
                var avgList = itemsForAvg.ToList();
                return new
                {
                    averageQuantity = avgList.Count == 0 ? 0 : Math.Round(avgList.Average(i => i.Quantity), 1),
                    itemCount = avgList.Count
                };

            case "compare_warehouses":
                return await BuildWarehouseComparisonAsync();

            default: // total_quantity
                var itemsForTotal = await itemRepository.GetAllAsync(warehouseId, intent.LowStockOnly, intent.ItemNameSearch);
                var totalList = itemsForTotal.ToList();
                return new
                {
                    totalQuantity = totalList.Sum(i => i.Quantity),
                    matchedAnyItem = totalList.Count > 0,
                    searchTerm = intent.ItemNameSearch
                };
        }
    }

    private async Task<object> BuildWarehouseComparisonAsync()
    {
        var warehouses = await warehouseRepository.GetAllAsync();
        var items = await itemRepository.GetAllAsync();
        var itemList = items.ToList();

        var stats = warehouses
            .Select(w => new
            {
                w.Name,
                TotalQuantity = itemList.Where(i => i.WarehouseId == w.Id).Sum(i => i.Quantity)
            })
            .ToList();

        if (stats.Count == 0)
            return new { warehouses = stats, highestStock = (object?)null, lowestStock = (object?)null };

        return new
        {
            warehouses = stats,
            highestStock = stats.OrderByDescending(s => s.TotalQuantity).First(),
            lowestStock = stats.OrderBy(s => s.TotalQuantity).First()
        };
    }

    private async Task<string> PhraseAnswerAsync(string question, object data)
    {
        var userContent = $"Question: {question}\nData: {JsonSerializer.Serialize(data)}";
        var messages = new List<object>
        {
            new { role = "system", content = AnswerSystemPrompt },
            new { role = "user", content = userContent }
        };

        var answer = await CallOpenAiAsync(messages, jsonMode: false);

        return string.IsNullOrWhiteSpace(answer)
            ? "I found the data but couldn't put together an answer. Try rephrasing."
            : answer;
    }

    private async Task<string?> CallOpenAiAsync(List<object> messages, bool jsonMode)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.openai.com/v1/");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", configuration["OpenAI:ApiKey"]);

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = "gpt-4o-mini",
            ["messages"] = messages
        };

        if (jsonMode)
            requestBody["response_format"] = new { type = "json_object" };

        var response = await client.PostAsJsonAsync("chat/completions", requestBody);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"OpenAI request failed ({response.StatusCode}): {errorBody}");
        }

        var completion = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>();
        return completion?.Choices.FirstOrDefault()?.Message.Content;
    }

    private record QueryIntent(string? Action, string? WarehouseName, bool LowStockOnly, string? ItemNameSearch);

    private record OpenAiChatResponse([property: JsonPropertyName("choices")] List<OpenAiChoice> Choices);

    private record OpenAiChoice([property: JsonPropertyName("message")] OpenAiMessage Message);

    private record OpenAiMessage([property: JsonPropertyName("content")] string Content);
}
