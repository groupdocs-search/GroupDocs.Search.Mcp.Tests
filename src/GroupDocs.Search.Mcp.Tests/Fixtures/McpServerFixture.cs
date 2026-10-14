using ModelContextProtocol.Client;
using Xunit;

namespace GroupDocs.Search.Mcp.IntegrationTests.Fixtures;

/// Boots the GroupDocs.Search.Mcp server as a child process, wires an MCP stdio client, and
/// seeds a temporary storage folder with sample documents. Shared across all tests in the same
/// xUnit collection.
///
/// Three launch channels, picked by environment variable — the first one set wins:
///
///   MCP_SERVER_IMAGE  docker run of that image. THE channel for this product: Search ships
///                     Docker-only (the packed tool exceeds NuGet.org's 250 MB limit), so the
///                     dnx channel below cannot resolve a package and its tests are skipped
///                     in CI. The storage folder is bind-mounted at /data, so the host-side
///                     assertions in these tests see exactly what the container wrote.
///   MCP_SERVER_DLL    dotnet &lt;dll&gt;, for testing a local build before it is published.
///   (neither)         dnx against the published NuGet package.
public sealed class McpServerFixture : IAsyncLifetime
{
    private const string ContainerStorage = "/data";
    private const string ContainerLicenseDir = "/license";

    public string StoragePath { get; } = Path.Combine(
        Path.GetTempPath(),
        $"gdsearch-mcp-it-{Guid.NewGuid():N}");

    public string PackageVersionUnderTest => PackageVersion.Value;

    /// Which launch channel this run used — named in failures so a red test says which
    /// artifact it was actually testing.
    public string Channel { get; private set; } = "dnx";

    /// A token for tool calls that are legitimately slow, overriding the MCP client's default
    /// per-request timeout. Indexing reads every document, and the first engine call in a fresh
    /// container also pays the engine's start-up cost, so add_to_index / update_index use this.
    public static CancellationToken Patience(TimeSpan? timeout = null) =>
        new CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(3)).Token;

    public McpClient Client { get; private set; } = null!;

    private static string? ServerImage => NullIfBlank(Environment.GetEnvironmentVariable("MCP_SERVER_IMAGE"));
    private static string? ServerDll => NullIfBlank(Environment.GetEnvironmentVariable("MCP_SERVER_DLL"));
    private static string? LicensePath => NullIfBlank(Environment.GetEnvironmentVariable("GROUPDOCS_LICENSE_PATH"));

    private static readonly string[] MeteredVariables =
    {
        "GROUPDOCS_METERED_PUBLIC_KEY",
        "GROUPDOCS_METERED_PRIVATE_KEY",
    };

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(StoragePath);
        // Real samples go to their own sub-folder; every test that needs controlled content
        // creates its own workspace next to it (SampleDocuments.NewWorkspace), so tests sharing
        // this server never index or modify each other's files.
        SampleDocuments.CopyRealSamples(
            Path.Combine(StoragePath, SampleDocuments.SamplesFolder), SampleDocuments.ResolveSourceSampleDocs());

        var options = BuildTransportOptions();

        var transport = new StdioClientTransport(options);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        Client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
    }

    private StdioClientTransportOptions BuildTransportOptions()
    {
        if (ServerImage is { } image)
        {
            Channel = $"docker:{image}";
            return new StdioClientTransportOptions
            {
                Name = "groupdocs-search-mcp",
                Command = CommandResolver.Resolve("docker"),
                Arguments = BuildDockerArguments(image),
                WorkingDirectory = StoragePath,
                // Values for the forwarded names are read from THIS process's environment by
                // `docker -e NAME`, so no key is ever written into an argument.
                EnvironmentVariables = BuildServerEnv(containerPaths: false),
            };
        }

        if (ServerDll is { } dll)
        {
            Channel = $"dll:{Path.GetFileName(dll)}";
            return new StdioClientTransportOptions
            {
                Name = "groupdocs-search-mcp",
                Command = CommandResolver.Resolve("dotnet"),
                Arguments = new[] { dll },
                WorkingDirectory = StoragePath,
                EnvironmentVariables = BuildServerEnv(containerPaths: false),
            };
        }

        // dnx has no `@latest` literal — to get the latest stable, omit the `@<version>` entirely.
        var packageSpec = PackageVersion.IsLatest
            ? "GroupDocs.Search.Mcp"
            : $"GroupDocs.Search.Mcp@{PackageVersion.Value}";

        Channel = $"dnx:{packageSpec}";
        return new StdioClientTransportOptions
        {
            Name = "groupdocs-search-mcp",
            Command = CommandResolver.Resolve("dnx"),
            Arguments = new[] { packageSpec, "--yes" },
            WorkingDirectory = StoragePath,
            EnvironmentVariables = BuildServerEnv(containerPaths: false),
        };
    }

    /// `docker run --rm -i` plus the storage mount, the in-container paths, and -e forwarding
    /// for whichever licensing variables this environment actually has.
    private string[] BuildDockerArguments(string image)
    {
        var args = new List<string> { "run", "--rm", "-i" };

        // The image runs as a non-root user (uid 1654). On a Linux host a bind mount keeps its
        // host owner, so a folder the runner owns is read-only to that user and every write -
        // the index under /data/.groupdocs-search-index - fails with "Permission denied".
        // Run as the host user, exactly as the README tells Linux users to. Docker Desktop on
        // Windows and macOS does not enforce bind-mount ownership, so the flag is Linux-only.
        if (OperatingSystem.IsLinux() && HostUser() is { } user)
        {
            args.Add("--user");
            args.Add(user);
        }

        args.AddRange(new[]
        {
            "-v", $"{StoragePath}:{ContainerStorage}",
            "-e", $"GROUPDOCS_MCP_STORAGE_PATH={ContainerStorage}",
        });

        foreach (var name in MeteredVariables)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
                continue;

            args.Add("-e");
            args.Add(name);              // name only: the value never reaches the command line
        }

        if (LicensePath is { } license && File.Exists(license))
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(license))!;
            args.Add("-v");
            args.Add($"{dir}:{ContainerLicenseDir}:ro");
            args.Add("-e");
            args.Add($"GROUPDOCS_LICENSE_PATH={ContainerLicenseDir}/{Path.GetFileName(license)}");
        }

        args.Add(image);
        return args.ToArray();
    }

    /// "uid:gid" of the current process, from `id`, or null when it cannot be read.
    private static string? HostUser()
    {
        static string? Id(string flag)
        {
            try
            {
                using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("id", flag)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                });
                var value = p!.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return p.ExitCode == 0 && value.Length > 0 ? value : null;
            }
            catch
            {
                return null;
            }
        }

        var uid = Id("-u");
        var gid = Id("-g");
        return uid is null || gid is null ? null : $"{uid}:{gid}";
    }

    private Dictionary<string, string?> BuildServerEnv(bool containerPaths)
    {
        var env = new Dictionary<string, string?>
        {
            ["GROUPDOCS_MCP_STORAGE_PATH"] = containerPaths ? ContainerStorage : StoragePath,
            ["DOTNET_NOLOGO"] = "true",
        };

        // Metered keys: forwarded by name so licensed-mode and metered tests can run in CI.
        // Their values are inherited from this process and are never logged.
        foreach (var name in MeteredVariables)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
                env[name] = value;
        }

        // Forward license path if present — enables licensed-mode tests in CI. On the docker
        // channel the in-container path is set by -e instead (see BuildDockerArguments).
        if (ServerImage is null && LicensePath is { } licensePath)
            env["GROUPDOCS_LICENSE_PATH"] = licensePath;

        return env;
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (Client is not null)
                await Client.DisposeAsync();
        }
        catch
        {
            // Swallow disposal errors — we don't want them to mask test failures.
        }

        try
        {
            if (Directory.Exists(StoragePath))
                Directory.Delete(StoragePath, recursive: true);
        }
        catch
        {
            // Best-effort cleanup on Windows where handles may linger briefly.
        }
    }
}

[CollectionDefinition(Name)]
public sealed class McpServerCollection : ICollectionFixture<McpServerFixture>
{
    public const string Name = "mcp-server";
}
