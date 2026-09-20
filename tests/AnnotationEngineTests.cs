using System.Text.Json;
using Xunit;

public class AnnotationEngineTests
{
    [Fact]
    public void ResolvesNestedArrayPath()
    {
        using var document = JsonDocument.Parse("{\"income\":{\"w2\":[{\"wages\":125000}]}}");
        using var data = JsonDocument.Parse("{\"path\":\"income.w2[0].wages\"}");

        var result = AnnotationEngine.ResolveValue(document.RootElement, "income.w2[0].wages", data.RootElement);

        Assert.Equal(125000m, result);
    }

    [Fact]
    public void UsesFallbackForMissingPath()
    {
        using var document = JsonDocument.Parse("{\"income\":{}}");
        using var data = JsonDocument.Parse("{\"path\":\"income.missing\",\"fallback\":\"N/A\"}");

        var result = AnnotationEngine.ResolveValue(document.RootElement, "income.missing", data.RootElement);

        Assert.Equal("N/A", result);
    }

    [Fact]
    public void FormatsCurrencyAndLeavesMissingValueBlank()
    {
        using var format = JsonDocument.Parse("{\"decimalPlaces\":0,\"thousandsSeparator\":true,\"currencySymbol\":\"$\"}");

        Assert.Equal("$125,000", AnnotationEngine.FormatValue(125000m, "currency", format.RootElement));
        Assert.Equal(string.Empty, AnnotationEngine.FormatValue(null, "currency", format.RootElement));
    }

    [Fact]
    public void ChecksValueBasedCheckbox()
    {
        using var annotation = JsonDocument.Parse("{\"value\":{\"equals\":\"single\"}}");

        Assert.True(AnnotationEngine.IsChecked("single", annotation.RootElement));
        Assert.False(AnnotationEngine.IsChecked("married", annotation.RootElement));
    }
}
