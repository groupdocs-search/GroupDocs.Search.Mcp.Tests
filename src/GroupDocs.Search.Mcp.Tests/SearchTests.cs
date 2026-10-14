using GroupDocs.Search.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Search.Mcp.IntegrationTests;

[Collection(McpServerCollection.Name)]
public class SearchTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SearchTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task SingleWord_FindsOnlyTheDocumentContainingIt()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "word");
        await _fixture.Index(ws);

        var result = await _fixture.Search(ws.IndexName, "revenue");
        _output.WriteLine(result.ToString());

        Assert.Equal(1, result.Found());
        Assert.Equal(ws.File(SampleDocuments.AlphaReport), Assert.Single(result.FilePaths()));
    }

    [Fact]
    public async Task OrQuery_FindsBothDocuments()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "or");
        await _fixture.Index(ws);

        var result = await _fixture.Search(ws.IndexName, "revenue OR renewal");

        Assert.Equal(2, result.Found());
    }

    [Fact]
    public async Task AndQuery_RequiresBothWords()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "and");
        await _fixture.Index(ws);

        Assert.Equal(1, (await _fixture.Search(ws.IndexName, "renewal AND parties")).Found());
        Assert.Equal(0, (await _fixture.Search(ws.IndexName, "renewal AND revenue")).Found());
    }

    [Fact]
    public async Task QuotedPhrase_Matches()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "phrase");
        await _fixture.Index(ws);

        var result = await _fixture.Search(ws.IndexName, "\"revenue forecast\"");

        Assert.Equal(1, result.Found());
    }

    [Fact]
    public async Task UnquotedPhrase_ReturnsZeroWithAHint()
    {
        // Engine behaviour on 26.10.0: unquoted multi-word input matches nothing. The server must
        // say why rather than leave the agent to conclude the text is absent.
        var ws = SampleDocuments.NewWorkspace(_fixture, "hint");
        await _fixture.Index(ws);

        var result = await _fixture.Search(ws.IndexName, "revenue forecast");

        Assert.Equal(0, result.Found());
        Assert.True(result.TryGetProperty("hint", out var hint), "Expected a hint for an unquoted phrase.");
        Assert.Contains("\"revenue forecast\"", hint.GetString());
    }

    [Fact]
    public async Task NoMatch_IsAnEmptyResultNotAnError()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "none");
        await _fixture.Index(ws);

        var result = await _fixture.Search(ws.IndexName, "zeppelin");

        Assert.Equal(0, result.Found());
        Assert.Empty(result.FilePaths());
    }

    [Fact]
    public async Task RealSamples_AreIndexedAcrossFormats()
    {
        var index = $"samples-{Guid.NewGuid().ToString("N")[..8]}";
        await _fixture.Ok("add_to_index", new()
        {
            ["documentsPath"] = SampleDocuments.SamplesFolder,
            ["indexName"] = index,
        }, slow: true);

        var result = await _fixture.Search(index, "lorem");
        var files = result.FilePaths();
        _output.WriteLine(string.Join(", ", files));

        Assert.Contains($"{SampleDocuments.SamplesFolder}/{SampleDocuments.LoremPdf}", files);
        Assert.Contains($"{SampleDocuments.SamplesFolder}/{SampleDocuments.LoremDocx}", files);
        Assert.Contains($"{SampleDocuments.SamplesFolder}/{SampleDocuments.LoremTxt}", files);
    }

    [Fact]
    public async Task Unlicensed_ResultCarriesTheEvaluationNote()
    {
        if (MeteredKeys.Configured || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GROUPDOCS_LICENSE_PATH")))
            return; // licensed run: the absence of the note is asserted by MeteredLicensingTests

        var ws = SampleDocuments.NewWorkspace(_fixture, "eval");
        await _fixture.Index(ws);

        var result = await _fixture.Search(ws.IndexName, "revenue");

        Assert.Contains("at most 10 documents", result.GetProperty("evaluation").GetString());
    }
}
