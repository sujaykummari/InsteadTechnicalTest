using System.Text.Json;

public class TaxRenderer
{
    public static void Main(string[] args)
    {
        var projectRoot = FindProjectRoot();
        using var taxpayer = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectRoot, "examples", "taxpayer-data.json")));
        var country = taxpayer.RootElement.GetProperty("country").GetString()?.ToUpperInvariant() ?? throw new InvalidOperationException("taxpayer-data.json must contain a country field.");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectRoot, "examples", "form-manifest.json")));
        var countries = manifest.RootElement.GetProperty("countries");
        if (!countries.TryGetProperty(country, out var route))
            throw new InvalidOperationException($"No tax form route is configured for country '{country}'.");

        var templatePath = Path.Combine(projectRoot, route.GetProperty("template").GetString()!);
        var annotationPath = Path.Combine(projectRoot, route.GetProperty("annotations").GetString()!);
        var defaultOutput = Path.Combine(projectRoot, route.GetProperty("output").GetString()!);
        var debug = args.Any(argument => string.Equals(argument, "--debug", StringComparison.OrdinalIgnoreCase));
        var outputArgument = args.FirstOrDefault(argument => !string.Equals(argument, "--debug", StringComparison.OrdinalIgnoreCase));
        var outputPath = outputArgument is not null ? Path.GetFullPath(outputArgument) : defaultOutput;
        if (debug && outputArgument is null)
            outputPath = Path.Combine(projectRoot, "output", Path.GetFileNameWithoutExtension(defaultOutput) + "-debug.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"The configured {country} tax form PDF is required.", templatePath);

        using var annotations = JsonDocument.Parse(File.ReadAllText(annotationPath));
        new PdfFormRenderer().Render(templatePath, outputPath, annotations, taxpayer, debug);

        Console.WriteLine($"Country:        {country}");
        Console.WriteLine($"Form:           {route.GetProperty("form").GetString()}");
        Console.WriteLine($"Original form:  {templatePath}");
        Console.WriteLine($"Annotations:    {annotationPath}");
        Console.WriteLine($"Debug overlay:  {debug}");
        Console.WriteLine($"Filled PDF:     {outputPath}");
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "examples")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the project root.");
    }
}