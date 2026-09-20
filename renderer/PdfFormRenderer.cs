using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Text.Json;

public sealed class PdfFormRenderer
{
    private const double DefaultTextVerticalOffset = 16;

    public void Render(string templatePath, string outputPath, JsonDocument annotationDocument, JsonDocument taxpayerDocument, bool debug = false)
    {
        using var pdf = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify);
        var form = annotationDocument.RootElement.GetProperty("form");
        foreach (var annotation in annotationDocument.RootElement.GetProperty("annotations").EnumerateArray())
        {
            var pageNumber = annotation.TryGetProperty("page", out var annotationPage)
                ? annotationPage.GetInt32()
                : form.GetProperty("page").GetInt32();
            if (pageNumber < 1 || pageNumber > pdf.Pages.Count)
                throw new InvalidOperationException($"Annotation page {pageNumber} is outside the template page range.");

            using var graphics = XGraphics.FromPdfPage(pdf.Pages[pageNumber - 1], XGraphicsPdfPageOptions.Append);
            var page = pdf.Pages[pageNumber - 1];
            var rectangle = ToPdfRectangle(annotation.GetProperty("position"), form, page.Width.Point, page.Height.Point);
            if (debug)
                DrawDebugRectangle(graphics, annotation, rectangle);
            DrawAnnotation(graphics, annotation, taxpayerDocument.RootElement, form, rectangle);
        }
        pdf.Save(outputPath);
    }

    private static void DrawAnnotation(XGraphics graphics, JsonElement annotation, JsonElement taxpayer, JsonElement form, XRect rectangle)
    {
        var data = annotation.GetProperty("data");
        var path = data.GetProperty("path").GetString() ?? throw new InvalidOperationException("Annotation path is missing.");
        var value = AnnotationEngine.ResolveValue(taxpayer, path, data);
        var type = annotation.GetProperty("type").GetString() ?? "text";
        if (type is "checkbox" or "radio")
        {
            if (AnnotationEngine.IsChecked(value, annotation))
            {
                var mark = annotation.TryGetProperty("appearance", out var appearance) && appearance.TryGetProperty("mark", out var markValue) ? markValue.GetString() ?? "X" : "X";
                graphics.DrawString(mark, new XFont("Helvetica", Math.Max(8, rectangle.Height * 0.9)), XBrushes.Black, rectangle, XStringFormats.Center);
            }
            return;
        }

        var format = annotation.TryGetProperty("format", out var configuredFormat) ? configuredFormat : default;
        var text = AnnotationEngine.FormatValue(value, type, format);
        if (text.Length == 0)
            return;
        var fontSize = format.ValueKind == JsonValueKind.Object && format.TryGetProperty("fontSize", out var configuredSize) && configuredSize.TryGetDouble(out var size) ? size : 9;
        var fontName = format.ValueKind == JsonValueKind.Object && format.TryGetProperty("fontFamily", out var configuredFont) ? configuredFont.GetString() ?? "Helvetica" : "Helvetica";
        var alignment = format.ValueKind == JsonValueKind.Object && format.TryGetProperty("alignment", out var configuredAlignment) ? configuredAlignment.GetString() : "left";
        var stringFormat = alignment switch
        {
            "center" => XStringFormats.Center,
            "right" => XStringFormats.CenterRight,
            _ => XStringFormats.CenterLeft
        };
        var verticalOffset = GetDouble(format, "verticalOffset", DefaultTextVerticalOffset);
        var textRectangle = new XRect(rectangle.X + 2, rectangle.Y + verticalOffset, Math.Max(0, rectangle.Width - 4), Math.Max(0, rectangle.Height - verticalOffset - 1));
        graphics.DrawString(text, new XFont(fontName, fontSize), XBrushes.Black, textRectangle, stringFormat);
    }

    private static XRect ToPdfRectangle(JsonElement position, JsonElement form, double actualWidth, double actualHeight)
    {
        var x = position.GetProperty("x").GetDouble();
        var y = position.GetProperty("y").GetDouble();
        var width = position.GetProperty("width").GetDouble();
        var height = position.GetProperty("height").GetDouble();
        var unit = position.TryGetProperty("unit", out var configuredUnit) ? configuredUnit.GetString() : null;
        if (unit == "relative" || form.GetProperty("coordinateSystem").GetString() == "relative")
        {
            x *= actualWidth;
            y *= actualHeight;
            width *= actualWidth;
            height *= actualHeight;
        }
        else
        {
            var coordinateWidth = form.GetProperty("width").GetDouble();
            var coordinateHeight = form.GetProperty("height").GetDouble();
            if (coordinateWidth <= 0 || coordinateHeight <= 0)
                throw new InvalidOperationException("Form coordinate dimensions must be greater than zero.");

            x *= actualWidth / coordinateWidth;
            y *= actualHeight / coordinateHeight;
            width *= actualWidth / coordinateWidth;
            height *= actualHeight / coordinateHeight;
        }
        return new XRect(x, y, width, height);
    }

    private static void DrawDebugRectangle(XGraphics graphics, JsonElement annotation, XRect rectangle)
    {
        var debugPen = new XPen(XColors.Red, 0.8) { DashStyle = XDashStyle.Dash };
        graphics.DrawRectangle(debugPen, rectangle);
        var id = annotation.GetProperty("id").GetString() ?? "annotation";
        var labelRectangle = new XRect(rectangle.X, Math.Max(0, rectangle.Y - 10), Math.Max(40, rectangle.Width), 10);
        graphics.DrawString(id, new XFont("Helvetica", 5), XBrushes.Red, labelRectangle, XStringFormats.TopLeft);
    }

    private static double GetDouble(JsonElement value, string property, double fallback)
    {
        return value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out var result) && result.TryGetDouble(out var number)
            ? number
            : fallback;
    }
}