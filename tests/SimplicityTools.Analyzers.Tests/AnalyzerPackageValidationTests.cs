using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace SimplicityTools.Analyzers.Tests;

public sealed class AnalyzerPackageValidationTests
{
    [Fact]
    public async Task PackedAnalyzerPackage_UsesAnalyzerLayout_AndReportsDiagnosticsInConsumer()
    {
        var validation = await PackAnalyzerPackageAsync();
        Assert.DoesNotContain("NU5128", validation.PackOutput, StringComparison.Ordinal);

        using (var archive = ZipFile.OpenRead(validation.PackagePath))
        {
            var entries = archive.Entries.Select(static entry => entry.FullName).ToArray();

            Assert.Contains("analyzers/dotnet/cs/SimplicityTools.Analyzers.dll", entries);
            Assert.Contains("build/SimplicityTools.Analyzers.props", entries);
            Assert.Contains("buildTransitive/SimplicityTools.Analyzers.props", entries);
            Assert.DoesNotContain(entries, static entry => entry.StartsWith("lib/", StringComparison.Ordinal));

            var nuspecEntry = archive.GetEntry("SimplicityTools.Analyzers.nuspec");
            Assert.NotNull(nuspecEntry);

            using var nuspecStream = nuspecEntry!.Open();
            var nuspec = XDocument.Load(nuspecStream);
            var ns = nuspec.Root!.Name.Namespace;
            var metadata = nuspec.Root.Element(ns + "metadata");

            Assert.NotNull(metadata);
            Assert.Equal("true", metadata!.Element(ns + "developmentDependency")?.Value);
            Assert.Null(metadata.Element(ns + "dependencies"));
            Assert.Equal(
                "Analyzer",
                metadata.Element(ns + "packageTypes")?
                    .Element(ns + "packageType")?
                    .Attribute("name")?
                    .Value);
        }

        var consumerDirectory = Path.Combine(validation.WorkingDirectory, "consumer");
        Directory.CreateDirectory(consumerDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(consumerDirectory, "Consumer.csproj"),
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="SimplicityTools.Analyzers" Version="{{validation.Version}}" />
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """);

        await File.WriteAllTextAsync(
            Path.Combine(consumerDirectory, "Feature.cs"),
            """
            namespace Consumer;

            public interface IPricer
            {
                decimal Price();
            }

            public sealed class DefaultPricer : IPricer
            {
                public decimal Price() => 12m;
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
        var buildOutput = $"{build.StandardOutput}{Environment.NewLine}{build.StandardError}";
        Assert.Contains("warning SF0001", buildOutput, StringComparison.Ordinal);
        Assert.Contains("warning SF0002", buildOutput, StringComparison.Ordinal);

        using var assets = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(consumerDirectory, "obj", "project.assets.json")));
        var targetPackage = assets.RootElement
            .GetProperty("targets")
            .EnumerateObject()
            .SelectMany(static target => target.Value.EnumerateObject())
            .Single(static library => library.Name.StartsWith("SimplicityTools.Analyzers/", StringComparison.Ordinal));

        Assert.False(targetPackage.Value.TryGetProperty("compile", out _));
        Assert.False(targetPackage.Value.TryGetProperty("runtime", out _));
        Assert.False(targetPackage.Value.TryGetProperty("dependencies", out _));
    }

    private static async Task<PackageValidationResult> PackAnalyzerPackageAsync()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(repositoryRoot, "artifacts", "analyzer-package-validation-tests");
        var packageSourceDirectory = Path.Combine(workingDirectory, "packages");
        // The directory must end in a "packages" segment: SF0002 maps assembly reference
        // paths back to package ids by looking for a "/packages/" marker.
        var globalPackagesDirectory = Path.Combine(workingDirectory, "nuget", "packages");
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
                Path.Combine("src", "SimplicityTools.Analyzers", "SimplicityTools.Analyzers.csproj"),
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
                $"Analyzer pack failed.{Environment.NewLine}{pack.StandardOutput}{Environment.NewLine}{pack.StandardError}");
        }

        var packagePath = Directory.GetFiles(packageSourceDirectory, $"SimplicityTools.Analyzers.{version}.nupkg", SearchOption.TopDirectoryOnly).Single();
        return new PackageValidationResult(
            version,
            workingDirectory,
            packageSourceDirectory,
            globalPackagesDirectory,
            packagePath,
            $"{pack.StandardOutput}{Environment.NewLine}{pack.StandardError}");
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
        string PackagePath,
        string PackOutput);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
