using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace SimplicityTools.Cli.Tests;

/// <summary>
/// Process-level helpers shared by the M3 CLI UX test files. Mirrors the helpers in
/// <see cref="AnalyzeCommandTests" /> without modifying that file.
/// </summary>
internal static class CliTestSupport
{
    private static readonly SemaphoreSlim BuildLock = new(1, 1);
    private static bool cliBuilt;

    public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    public static async Task<ProcessResult> RunCliAsync(params string[] arguments)
    {
        await BuildCliAsync();
        return await RunProcessAsync("dotnet", [GetCliAssemblyPath(), .. arguments], GetRepositoryRoot());
    }

    public static async Task<ProcessResult> RunProcessAsync(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
    {
        using var process = new Process
        {
            StartInfo =
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start process '{fileName}'.");
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new ProcessResult(process.ExitCode, await standardOutput, await standardError);
    }

    public static async Task BuildCliAsync()
    {
        if (cliBuilt)
        {
            return;
        }

        await BuildLock.WaitAsync();

        try
        {
            if (cliBuilt)
            {
                return;
            }

            var result = await RunProcessAsync(
                "dotnet",
                ["build", GetRepositoryPath("src", "SimplicityTools.Cli", "SimplicityTools.Cli.csproj"), "--nologo", "--verbosity", "quiet"],
                GetRepositoryRoot());

            Assert.Equal(0, result.ExitCode);
            cliBuilt = true;
        }
        finally
        {
            BuildLock.Release();
        }
    }

    public static string GetCliAssemblyPath()
    {
        return GetRepositoryPath("src", "SimplicityTools.Cli", "bin", "Debug", "net10.0", "SimplicityTools.Cli.dll");
    }

    public static string CreateSampleWorkspace(string sampleDirectoryName)
    {
        var sourceDirectory = GetRepositoryPath("samples", sampleDirectoryName);
        var workspace = GetRepositoryPath("tests", "SimplicityTools.Cli.Tests", ".workspace", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        CopyDirectory(sourceDirectory, workspace);
        return workspace;
    }

    public static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    public static string GetRepositoryPath(params string[] segments)
    {
        return Path.Combine([GetRepositoryRoot(), .. segments]);
    }

    public static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SimplicityTools.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root from the test base directory.");
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            if (IsBuildArtifact(relativePath))
            {
                // Stale bin/obj from in-place sample analysis breaks restore in the copy.
                continue;
            }

            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
    }

    private static bool IsBuildArtifact(string relativePath)
    {
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        return relativePath.Split(separators, StringSplitOptions.RemoveEmptyEntries).Any(static segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }
}
