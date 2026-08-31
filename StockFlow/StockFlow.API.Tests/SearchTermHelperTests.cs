using StockFlow.API.Services;
using Xunit;

namespace StockFlow.API.Tests;

public class SearchTermHelperTests
{
    // The real contract: whatever this returns still needs to be found INSIDE the real
    // item name via a SQL "contains" search. This is the exact bug that shipped:
    // searching "mouses" found nothing because "mouses" is not a substring of "Mouse".
    [Theory]
    [InlineData("mouses", "Wireless Mouse")]
    [InlineData("boxes", "Storage Box")]
    [InlineData("cables", "USB-C Cable")]
    public void Singularize_StaysAContainedSubstringOfTheRealItemName(string pluralSearchTerm, string realItemName)
    {
        var singular = SearchTermHelper.Singularize(pluralSearchTerm);

        Assert.Contains(singular, realItemName, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("keyboard")]
    [InlineData("monitor")]
    public void Singularize_LeavesNonPluralWordsUnchanged(string input)
    {
        Assert.Equal(input, SearchTermHelper.Singularize(input));
    }

    // Short words are left alone so we don't strip "gas" down to "ga" and start
    // matching unrelated items on a two-letter fragment.
    [Theory]
    [InlineData("as")]
    [InlineData("gas")]
    public void Singularize_DoesNotMangleShortWords(string input)
    {
        Assert.Equal(input, SearchTermHelper.Singularize(input));
    }
}
