namespace StockFlow.API.Services;

public static class SearchTermHelper
{
    public static string Singularize(string term)
    {
        if (term.EndsWith("es", StringComparison.OrdinalIgnoreCase) && term.Length > 4)
            return term[..^2];

        if (term.EndsWith("s", StringComparison.OrdinalIgnoreCase) && term.Length > 3)
            return term[..^1];

        return term;
    }
}
