using System.Diagnostics;
using System.IO.Compression;
using Xunit;

namespace SimplicityTools.Metrics.Tests;

public sealed class MetricsPackageValidationTests
{
    [Fact]
    public async Task PackedMetricsPackage_ShipsOnlyLibraryAssets_WithXmlDocs_AndBuildsInAConsumer()
    {
        var validation = await PackMetricsPackageAsync();

        using (var archive = ZipFile.OpenRead(validation.PackagePath))
        {
            var entries = archive.Entries.Select(static entry => entry.FullName).ToArray();

            Assert.Contains("README.md", entries);
            Assert.Contains("simplicitytools-icon.png", entries);
            Assert.Contains("lib/net10.0/SimplicityTools.Metrics.dll", entries);
            Assert.Contains("lib/net10.0/SimplicityTools.Metrics.xml", entries);

            var libraryAssets = entries
                .Where(static entry => entry.StartsWith("lib/", StringComparison.Ordinal))
                .OrderBy(static entry => entry, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "lib/net10.0/SimplicityTools.Metrics.dll",
                    "lib/net10.0/SimplicityTools.Metrics.xml",
                    "lib/net8.0/SimplicityTools.Metrics.dll",
                    "lib/net8.0/SimplicityTools.Metrics.xml"
                ],
                libraryAssets);
        }

        var consumerDirectory = Path.Combine(validation.WorkingDirectory, "consumer");
        Directory.CreateDirectory(consumerDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(consumerDirectory, "Consumer.csproj"),
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="SimplicityTools.Metrics" Version="{{validation.Version}}" />
              </ItemGroup>
            </Project>
            """);

        await File.WriteAllTextAsync(
            Path.Combine(consumerDirectory, "Program.cs"),
            """
            using SimplicityTools.Metrics;

            namespace Consumer;

            [PrimaryPath]
            public sealed class CheckoutFlow
            {
            }

            public static class Program
            {
                public static async Task Main()
                {
                    ISimplicityCollector collector = new SimplicityCollector();
                    var snapshot = SimplicitySnapshot.Empty();

                    _ = snapshot.PrimaryPathRatio;
                    _ = snapshot.PrematureAbstractionRatio;
                    _ = snapshot.ToSummary();
                    _ = await collector.CollectAsync("consumer.sln");
                }
            }
            """);

        var build = await RunDotNetAsync(
            consumerDirectory,
            validation.GlobalPackagesDirectory,
            [
                "build",
                "Consumer.csproj",
                "--verbosity",
                "minimal",
                $"-p:RestoreAdditionalProjectSources={validation.PackageSourceDirectory}"
            ]);

        Assert.True(
            build.ExitCode == 0,
            $"Downstream consumer build failed.{Environment.NewLine}{build.StandardOutput}{Environment.NewLine}{build.StandardError}");
    }

    private static async Task<PackageValidationResult> PackMetricsPackageAsync()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(repositoryRoot, "artifacts", "metrics-package-validation-tests");
        var packageSourceDirectory = Path.Combine(workingDirectory, "packages");
        var globalPackagesDirectory = Path.Combine(workingDirectory, "global-packages");
        var version = $"0.4.0-packagevalidation.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }

        Directory.CreateDirectory(packageSourceDirectory);
        Directory.CreateDirectory(globalPackagesDirectory);

        var pack = await RunDotNetAsync(
            repositoryRoot,
            globalPackagesDirectory,
            [
                "pack",
                Path.Combine("src", "SimplicityTools.Metrics", "SimplicityTools.Metrics.csproj"),
                "--configuration",
                "Release",
                "--output",
                packageSourceDirectory,
                "--verbosity",
                "minimal",
                $"-p:Version={version}"
            ]);

        if (pack.ExitCode != 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"Metrics pack failed.{Environment.NewLine}{pack.StandardOutput}{Environment.NewLine}{pack.StandardError}");
        }

        var packagePath = Directory.GetFiles(packageSourceDirectory, $"SimplicityTools.Metrics.{version}.nupkg", SearchOption.TopDirectoryOnly).Single();
        return new PackageValidationResult(version, workingDirectory, packageSourceDirectory, globalPackagesDirectory, packagePath);
    }

    private static async Task<ProcessResult> RunDotNetAsync(string workingDirectory, string globalPackagesDirectory, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["NUGET_PACKAGES"] = globalPackagesDirectory;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new ProcessResult(process.ExitCode, await standardOutputTask, await standardErrorTask);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SimplicityTools.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root from the test base directory.");
    }

    private sealed record PackageValidationResult(
        string Version,
        string WorkingDirectory,
        string PackageSourceDirectory,
        string GlobalPackagesDirectory,
        string PackagePath);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
