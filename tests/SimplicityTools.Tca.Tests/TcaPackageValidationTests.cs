using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace SimplicityTools.Tca.Tests;

public sealed class TcaPackageValidationTests
{
    [Fact]
    public async Task PackedTcaPackage_ShipsOnlyLibraryAssets_DeclaresLibraryDependencies_AndBuildsInAConsumer()
    {
        var validation = await PackLibraryPackagesAsync();

        using (var archive = ZipFile.OpenRead(validation.TcaPackagePath))
        {
            var entries = archive.Entries.Select(static entry => entry.FullName).ToArray();

            Assert.Contains("README.md", entries);
            Assert.Contains("simplicitytools-icon.png", entries);
            Assert.Contains("lib/net10.0/SimplicityTools.Tca.dll", entries);
            Assert.Contains("lib/net10.0/SimplicityTools.Tca.xml", entries);

            var libraryAssets = entries
                .Where(static entry => entry.StartsWith("lib/", StringComparison.Ordinal))
                .OrderBy(static entry => entry, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "lib/net10.0/SimplicityTools.Tca.dll",
                    "lib/net10.0/SimplicityTools.Tca.xml",
                    "lib/net8.0/SimplicityTools.Tca.dll",
                    "lib/net8.0/SimplicityTools.Tca.xml"
                ],
                libraryAssets);

            var nuspecEntry = archive.GetEntry("SimplicityTools.Tca.nuspec");
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

            // One dependency group per shipped target framework, each declaring both library dependencies.
            Assert.Collection(
                dependencyGroups!,
                group => Assert.Equal("net10.0", group.Attribute("targetFramework")?.Value),
                group => Assert.Equal("net8.0", group.Attribute("targetFramework")?.Value));

            foreach (var group in dependencyGroups!)
            {
                Assert.Collection(
                    group.Elements()
                        .OrderBy(static dependency => dependency.Attribute("id")?.Value, StringComparer.Ordinal),
                    dependency =>
                    {
                        Assert.Equal(ns + "dependency", dependency.Name);
                        Assert.Equal("SimplicityTools.Filters", dependency.Attribute("id")?.Value);
                        Assert.Equal(validation.Version, dependency.Attribute("version")?.Value);
                    },
                    dependency =>
                    {
                        Assert.Equal(ns + "dependency", dependency.Name);
                        Assert.Equal("SimplicityTools.Metrics", dependency.Attribute("id")?.Value);
                        Assert.Equal(validation.Version, dependency.Attribute("version")?.Value);
                    });
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
                <PackageReference Include="SimplicityTools.Tca" Version="{{validation.Version}}" />
              </ItemGroup>
            </Project>
            """);

        await File.WriteAllTextAsync(
            Path.Combine(consumerDirectory, "Program.cs"),
            """
            using SimplicityTools.Filters;
            using SimplicityTools.Metrics;
            using SimplicityTools.Tca;

            namespace Consumer;

            public static class Program
            {
                public static void Main()
                {
                    var snapshot = new SimplicitySnapshot
                    {
                        TotalProjects = 3,
                        TotalFiles = 18,
                        PrimaryPathFileCount = 9,
                        AbstractionLayerCount = 2,
                        ExternalDependencyCount = 2,
                        UnusedDependencyCount = 0,
                        InterfacesWithSingleImplementation = 1,
                        AverageMethodComplexity = 2.5,
                        EstimatedOnboardingTime = TimeSpan.FromHours(10),
                        CollectedAt = DateTimeOffset.Parse("2026-04-30T19:09:43.583-04:00")
                    };

                    var verdicts = new[]
                    {
                        TwoAmTestEvaluator.Evaluate(snapshot),
                        HalfRuleEvaluator.Evaluate(snapshot),
                        PrimaryPathFirstEvaluator.Evaluate(snapshot)
                    };

                    var estimate = TcaEstimate.Create(snapshot, verdicts);
                    _ = estimate.TotalPerYear;
                    _ = estimate.ToExecutiveSummary();
                    _ = TcaInputs.Defaults;
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
                library.Name.StartsWith("SimplicityTools.Tca/", StringComparison.Ordinal) ||
                library.Name.StartsWith("SimplicityTools.Filters/", StringComparison.Ordinal) ||
                library.Name.StartsWith("SimplicityTools.Metrics/", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(3, targetPackages.Length);

        var tcaTarget = Assert.Single(targetPackages, static package => package.Name.StartsWith("SimplicityTools.Tca/", StringComparison.Ordinal));
        var filtersTarget = Assert.Single(targetPackages, static package => package.Name.StartsWith("SimplicityTools.Filters/", StringComparison.Ordinal));
        var metricsTarget = Assert.Single(targetPackages, static package => package.Name.StartsWith("SimplicityTools.Metrics/", StringComparison.Ordinal));

        Assert.True(
            tcaTarget.Value.TryGetProperty("dependencies", out var tcaDependencies) &&
            tcaDependencies.TryGetProperty("SimplicityTools.Filters", out _) &&
            tcaDependencies.TryGetProperty("SimplicityTools.Metrics", out _),
            "Tca package should restore with SimplicityTools.Filters and SimplicityTools.Metrics dependencies in the consumer assets graph.");

        Assert.True(
            filtersTarget.Value.TryGetProperty("dependencies", out var filtersDependencies) &&
            filtersDependencies.TryGetProperty("SimplicityTools.Metrics", out _),
            "Filters package should restore with a SimplicityTools.Metrics dependency in the consumer assets graph.");

        Assert.True(tcaTarget.Value.TryGetProperty("compile", out _));
        Assert.True(filtersTarget.Value.TryGetProperty("compile", out _));
        Assert.True(metricsTarget.Value.TryGetProperty("compile", out _));
    }

    private static async Task<PackageValidationResult> PackLibraryPackagesAsync()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(repositoryRoot, "artifacts", "tca-package-validation-tests");
        var packageSourceDirectory = Path.Combine(workingDirectory, "packages");
        var globalPackagesDirectory = Path.Combine(workingDirectory, "global-packages");
        var version = $"0.4.0-packagevalidation.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }

        Directory.CreateDirectory(packageSourceDirectory);
        Directory.CreateDirectory(globalPackagesDirectory);

        foreach (var projectPath in new[]
                 {
                     Path.Combine("src", "SimplicityTools.Metrics", "SimplicityTools.Metrics.csproj"),
                     Path.Combine("src", "SimplicityTools.Filters", "SimplicityTools.Filters.csproj"),
                     Path.Combine("src", "SimplicityTools.Tca", "SimplicityTools.Tca.csproj")
                 })
        {
            var pack = await RunDotNetAsync(
                repositoryRoot,
                globalPackagesDirectory,
                [
                    "pack",
                    projectPath,
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
                    $"Library pack failed for '{projectPath}'.{Environment.NewLine}{pack.StandardOutput}{Environment.NewLine}{pack.StandardError}");
            }
        }

        var tcaPackagePath = Directory.GetFiles(packageSourceDirectory, $"SimplicityTools.Tca.{version}.nupkg", SearchOption.TopDirectoryOnly).Single();
        return new PackageValidationResult(version, workingDirectory, packageSourceDirectory, globalPackagesDirectory, tcaPackagePath);
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
        string TcaPackagePath);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
