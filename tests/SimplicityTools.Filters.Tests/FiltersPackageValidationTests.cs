using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace SimplicityTools.Filters.Tests;

public sealed class FiltersPackageValidationTests
{
    [Fact]
    public async Task PackedFiltersPackage_ShipsOnlyLibraryAssets_DeclaresMetricsDependency_AndBuildsInAConsumer()
    {
        var validation = await PackLibraryPackagesAsync();

        using (var archive = ZipFile.OpenRead(validation.FiltersPackagePath))
        {
            var entries = archive.Entries.Select(static entry => entry.FullName).ToArray();

            Assert.Contains("README.md", entries);
            Assert.Contains("simplicitytools-icon.png", entries);
            Assert.Contains("lib/net10.0/SimplicityTools.Filters.dll", entries);
            Assert.Contains("lib/net10.0/SimplicityTools.Filters.xml", entries);

            var libraryAssets = entries
                .Where(static entry => entry.StartsWith("lib/", StringComparison.Ordinal))
                .OrderBy(static entry => entry, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "lib/net10.0/SimplicityTools.Filters.dll",
                    "lib/net10.0/SimplicityTools.Filters.xml",
                    "lib/net8.0/SimplicityTools.Filters.dll",
                    "lib/net8.0/SimplicityTools.Filters.xml"
                ],
                libraryAssets);

            var nuspecEntry = archive.GetEntry("SimplicityTools.Filters.nuspec");
            Assert.NotNull(nuspecEntry);

            using var nuspecStream = nuspecEntry!.Open();
            var nuspec = XDocument.Load(nuspecStream);
            var ns = nuspec.Root!.Name.Namespace;
            var metadata = nuspec.Root.Element(ns + "metadata");
            var dependencyGroups = metadata?
                .Element(ns + "dependencies")?
                .Elements(ns + "group")
                .OrderBy(static group => group.Attribute("targetFramework")?.Value, StringComparer.Ordinal)
                .ToArray();

            Assert.NotNull(dependencyGroups);

            // One dependency group per shipped target framework, each declaring the Metrics dependency.
            Assert.Collection(
                dependencyGroups!,
                group => Assert.Equal("net10.0", group.Attribute("targetFramework")?.Value),
                group => Assert.Equal("net8.0", group.Attribute("targetFramework")?.Value));

            foreach (var group in dependencyGroups!)
            {
                var metricsDependency = Assert.Single(
                    group.Elements(),
                    dependency => dependency.Name == ns + "dependency" &&
                                  string.Equals(dependency.Attribute("id")?.Value, "SimplicityTools.Metrics", StringComparison.Ordinal));

                Assert.Equal(validation.Version, metricsDependency.Attribute("version")?.Value);
            }
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
                <PackageReference Include="SimplicityTools.Filters" Version="{{validation.Version}}" />
              </ItemGroup>
            </Project>
            """);

        await File.WriteAllTextAsync(
            Path.Combine(consumerDirectory, "Program.cs"),
            """
            using SimplicityTools.Filters;
            using SimplicityTools.Metrics;

            namespace Consumer;

            public static class Program
            {
                public static void Main()
                {
                    var snapshot = new SimplicitySnapshot
                    {
                        TotalProjects = 2,
                        TotalFiles = 12,
                        PrimaryPathFileCount = 6,
                        AbstractionLayerCount = 1,
                        ExternalDependencyCount = 1,
                        UnusedDependencyCount = 0,
                        InterfacesWithSingleImplementation = 0,
                        AverageMethodComplexity = 2,
                        EstimatedOnboardingTime = TimeSpan.FromHours(8),
                        CollectedAt = DateTimeOffset.Parse("2026-04-30T19:09:43.583-04:00")
                    };

                    var verdicts = new[]
                    {
                        TwoAmTestEvaluator.Evaluate(snapshot),
                        HalfRuleEvaluator.Evaluate(snapshot),
                        PrimaryPathFirstEvaluator.Evaluate(snapshot)
                    };

                    _ = verdicts.All(static verdict => verdict.Passes);
                    _ = snapshot.ToSummary();
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

        using var assets = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(consumerDirectory, "obj", "project.assets.json")));
        var targetPackages = assets.RootElement
            .GetProperty("targets")
            .EnumerateObject()
            .SelectMany(static target => target.Value.EnumerateObject())
            .Where(static library =>
                library.Name.StartsWith("SimplicityTools.Filters/", StringComparison.Ordinal) ||
                library.Name.StartsWith("SimplicityTools.Metrics/", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(2, targetPackages.Length);

        var filtersTarget = Assert.Single(targetPackages, static package => package.Name.StartsWith("SimplicityTools.Filters/", StringComparison.Ordinal));
        var metricsTarget = Assert.Single(targetPackages, static package => package.Name.StartsWith("SimplicityTools.Metrics/", StringComparison.Ordinal));

        Assert.True(
            filtersTarget.Value.TryGetProperty("dependencies", out var resolvedDependencies) &&
            resolvedDependencies.TryGetProperty("SimplicityTools.Metrics", out _),
            "Filters package should restore with a SimplicityTools.Metrics dependency in the consumer assets graph.");

        Assert.True(metricsTarget.Value.TryGetProperty("compile", out _));
    }

    private static async Task<PackageValidationResult> PackLibraryPackagesAsync()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(repositoryRoot, "artifacts", "filters-package-validation-tests");
        var packageSourceDirectory = Path.Combine(workingDirectory, "packages");
        var globalPackagesDirectory = Path.Combine(workingDirectory, "global-packages");
        var version = $"0.4.0-packagevalidation.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }

        Directory.CreateDirectory(packageSourceDirectory);
        Directory.CreateDirectory(globalPackagesDirectory);

        var buildOutputDirectory = Path.Combine(workingDirectory, "build-output");

        foreach (var (projectPath, projectSlug) in new[]
                 {
                     (Path.Combine("src", "SimplicityTools.Metrics", "SimplicityTools.Metrics.csproj"), "metrics"),
                     (Path.Combine("src", "SimplicityTools.Filters", "SimplicityTools.Filters.csproj"), "filters")
                 })
        {
            var pack = await RunDotNetAsync(
                repositoryRoot,
                nugetPackagesDirectory: null,
                [
                    "pack",
                    projectPath,
                    "--configuration",
                    "Release",
                    "--output",
                    packageSourceDirectory,
                    "--verbosity",
                    "minimal",
                    $"-p:Version={version}",
                    $"-p:BaseOutputPath={Path.Combine(buildOutputDirectory, projectSlug)}{Path.DirectorySeparatorChar}",
                    "-p:ProduceReferenceAssembly=false"
                ]);

            if (pack.ExitCode != 0)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Library pack failed for '{projectPath}'.{Environment.NewLine}{pack.StandardOutput}{Environment.NewLine}{pack.StandardError}");
            }
        }

        var filtersPackagePath = Directory.GetFiles(packageSourceDirectory, $"SimplicityTools.Filters.{version}.nupkg", SearchOption.TopDirectoryOnly).Single();
        return new PackageValidationResult(version, workingDirectory, packageSourceDirectory, globalPackagesDirectory, filtersPackagePath);
    }

    private static async Task<ProcessResult> RunDotNetAsync(string workingDirectory, string? nugetPackagesDirectory, IReadOnlyList<string> arguments)
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

        if (nugetPackagesDirectory is not null)
        {
            startInfo.Environment["NUGET_PACKAGES"] = nugetPackagesDirectory;
        }

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
        string FiltersPackagePath);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
