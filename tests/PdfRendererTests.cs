using System;
using System.IO;
using System.Text.Json;
using PdfSharpCore.Pdf.IO;
using Xunit;

public class PdfRendererTests
{
    [Fact]
    public void ManifestContainsSeparateUsAndIndiaRoutes()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(GetProjectRoot(), "examples", "form-manifest.json")));
        var countries = manifest.RootElement.GetProperty("countries");

        Assert.Equal("us-1040", countries.GetProperty("US").GetProperty("form").GetString());
        Assert.Equal("india-form-16", countries.GetProperty("IN").GetProperty("form").GetString());
        Assert.NotEqual(countries.GetProperty("US").GetProperty("template").GetString(), countries.GetProperty("IN").GetProperty("template").GetString());
        Assert.EndsWith("india-tax-form-blank.pdf", countries.GetProperty("IN").GetProperty("template").GetString());
    }

    [Fact]
    public void RendersFilledPdfFromOriginalIrsForm()
    {
        var directory = Path.Combine(Path.GetTempPath(), "instead-tax-annotation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var outputPath = Path.Combine(directory, "filled.pdf");
        var templatePath = Path.Combine(GetProjectRoot(), "templates", "us-tax-form.pdf");
        using var annotations = JsonDocument.Parse(File.ReadAllText(Path.Combine(GetProjectRoot(), "examples", "form-1040.json")));
        using var taxpayer = JsonDocument.Parse(File.ReadAllText(Path.Combine(GetProjectRoot(), "examples", "taxpayer-data.json")));
        new PdfFormRenderer().Render(templatePath, outputPath, annotations, taxpayer);

        Assert.True(File.Exists(outputPath));
        var header = Convert.ToHexString(File.ReadAllBytes(outputPath)[..5]);
        Assert.Equal("255044462D", header);
        Assert.True(new FileInfo(outputPath).Length > new FileInfo(templatePath).Length);
    }

    [Fact]
    public void OriginalIrsPageMatchesUsAnnotationCoordinateSpace()
    {
        var root = GetProjectRoot();
        using var pdf = PdfReader.Open(Path.Combine(root, "templates", "us-tax-form.pdf"), PdfDocumentOpenMode.ReadOnly);
        using var annotations = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "examples", "form-1040.json")));
        var form = annotations.RootElement.GetProperty("form");

        Assert.True(pdf.Pages.Count >= 2);
        Assert.Equal(form.GetProperty("width").GetDouble(), pdf.Pages[0].Width.Point, 1);
        Assert.Equal(form.GetProperty("height").GetDouble(), pdf.Pages[0].Height.Point, 1);
    }

    private static string GetProjectRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "examples")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Project root not found.");
    }
}
