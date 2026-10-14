namespace GroupDocs.Search.Mcp.IntegrationTests.Fixtures;

/// Fixtures for a stateful server. Two kinds:
///
/// * Real documents committed under the repo's `Files/` folder — the upstream
///   GroupDocs.Search-for-.NET `Examples/Resources/Documents` set (see Files/README.md). They are
///   copied once into `<storage>/samples/` and prove multi-format indexing (DOCX, PDF, TXT).
/// * Synthetic text files written per test into a private workspace sub-folder, whose words the
///   assertions depend on. Search tests must vary what the engine indexes — the words — so every
///   expected hit and miss is controlled here, never inferred from a real sample's content.
internal static class SampleDocuments
{
    public const string SamplesFolder = "samples";

    // Real samples (Files/). "lorem" appears in all three Lorem ipsum files.
    public const string LoremDocx = "Lorem ipsum.docx";
    public const string LoremPdf  = "Lorem ipsum.pdf";
    public const string LoremTxt  = "Lorem ipsum.txt";
    public const string EnglishDocx = "English.docx";
    public const string EnglishTxt  = "English.txt";

    public static IReadOnlyList<string> RealSamples { get; } = new[]
    {
        LoremDocx, LoremPdf, LoremTxt, EnglishDocx, EnglishTxt,
    };

    // Synthetic content. Each file owns words no other file contains.
    public const string AlphaReport = "alpha-report.txt";
    public const string AlphaText =
        "Quarterly revenue grew by twelve percent in the third quarter.\n" +
        "The revenue forecast for next year is stable.\n";

    public const string BetaNotes = "beta-notes.txt";
    public const string BetaText =
        "Notes on the contract renewal meeting.\n" +
        "The renewal terms were accepted by both parties.\n";

    /// A private folder (relative to storage) holding the two synthetic files, and a unique index
    /// name — so a test can add, edit and remove without touching any other test's state.
    public static Workspace NewWorkspace(McpServerFixture fixture, string label)
    {
        var id = $"{label}-{Guid.NewGuid().ToString("N")[..8]}";
        var relative = $"ws-{id}";
        var full = Path.Combine(fixture.StoragePath, relative);
        Directory.CreateDirectory(full);
        File.WriteAllText(Path.Combine(full, AlphaReport), AlphaText);
        File.WriteAllText(Path.Combine(full, BetaNotes), BetaText);
        return new Workspace(relative, full, $"idx-{id}");
    }

    public static void CopyRealSamples(string targetDirectory, string? sourceDirectory)
    {
        if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return;

        Directory.CreateDirectory(targetDirectory);
        foreach (var name in RealSamples)
        {
            var src = Path.Combine(sourceDirectory, name);
            if (File.Exists(src))
                File.Copy(src, Path.Combine(targetDirectory, name), overwrite: true);
        }
    }

    /// Resolves the source folder containing real sample files. Order:
    ///   1. GROUPDOCS_MCP_SAMPLE_DOCS env var (set by docker-compose mount).
    ///   2. `Files/` next to the test assembly — populated by the csproj copy item.
    ///   3. Walk up from the assembly to find the repo's `Files/`.
    public static string? ResolveSourceSampleDocs()
    {
        var env = Environment.GetEnvironmentVariable("GROUPDOCS_MCP_SAMPLE_DOCS");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(env))
            return env;

        var staged = Path.Combine(AppContext.BaseDirectory, "Files");
        if (Directory.Exists(staged))
            return staged;

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, "Files");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}

/// <param name="Relative">Folder path relative to the server's storage root, as tools expect it.</param>
/// <param name="Full">Absolute host path, for editing files between tool calls.</param>
/// <param name="IndexName">Index name private to this workspace.</param>
internal sealed record Workspace(string Relative, string Full, string IndexName)
{
    public string File(string name) => $"{Relative}/{name}";
}
