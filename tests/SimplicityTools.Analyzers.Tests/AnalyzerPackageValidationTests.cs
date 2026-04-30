using System.Diagnostics;
using System.IO.Compression;
using Xunit;

namespace SimplicityTools.Analyzers.Tests;

public sealed class AnalyzerPackageValidationTests
{
    [Fact]
    public async Task PackedAnalyzerPackage_UsesAnalyzerLayout_AndReportsDiagnosticsInConsumer()
    {
        var validation = await PackAnalyzerPackageAsync();

        using (var archive = ZipFile.OpenRead(validation.PackagePath))
        {
            var entries = archive.Entries.Select(static entry => entry.FullName).ToArray();

            Assert.Contains("analyzers/dotnet/cs/SimplicityTools.Analyzers.dll", entries);
            Assert.DoesNotContain("lib/net10.0/SimplicityTools.Analyzers.dll", entries);
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
        Assert.Contains("warning SF0001", $"{build.StandardOutput}{Environment.NewLine}{build.StandardError}", StringComparison.Ordinal);
    }

    private static async Task<PackageValidationResult> PackAnalyzerPackageAsync()
    {
        var repositoryRoot = GetRepositoryRoot();
        var workingDirectory = Path.Combine(repositoryRoot, "artifacts", "analyzer-package-validation-tests");
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
