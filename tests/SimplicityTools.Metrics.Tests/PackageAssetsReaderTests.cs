using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Metrics.Tests;

public sealed class PackageAssetsReaderTests : IDisposable
{
    private readonly string projectDirectory;
    private readonly string projectPath;

    public PackageAssetsReaderTests()
    {
        projectDirectory = Path.Combine(Path.GetTempPath(), $"assets-reader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(projectDirectory, "obj"));
        projectPath = Path.Combine(projectDirectory, "App.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }

    [Fact]
    public void TryRead_ReturnsNull_WhenAssetsFileIsMissing()
    {
        Assert.Null(PackageAssetsReader.TryRead(projectPath));
    }

    [Fact]
    public void TryRead_ReturnsNull_WhenAssetsFileIsCorrupt()
    {
        WriteAssetsFile("{ not json");

        Assert.Null(PackageAssetsReader.TryRead(projectPath));
    }

    [Fact]
    public void TryRead_ReadsDirectDependenciesAndAssemblies_SkippingAutoReferencedAndBuildOnly()
    {
        WriteAssetsFile("""
        {
          "version": 3,
          "targets": {
            "net10.0": {
              "Newtonsoft.Json/13.0.3": {
                "type": "package",
                "compile": { "lib/net6.0/Newtonsoft.Json.dll": {} },
                "runtime": { "lib/net6.0/Newtonsoft.Json.dll": {} }
              },
              "coverlet.collector/6.0.4": {
                "type": "package"
              },
              "PlaceholderOnly/1.0.0": {
                "type": "package",
                "compile": { "lib/netstandard2.0/_._": {} }
              },
              "MetaPackage/1.0.0": {
                "type": "package",
                "dependencies": { "Newtonsoft.Json": "13.0.3" }
              },
              "Microsoft.NETCore.App/10.0.0": {
                "type": "package",
                "compile": { "ref/net10.0/System.Runtime.dll": {} }
              }
            }
          },
          "project": {
            "frameworks": {
              "net10.0": {
                "dependencies": {
                  "Newtonsoft.Json": { "target": "Package", "version": "[13.0.3, )" },
                  "coverlet.collector": { "target": "Package", "version": "[6.0.4, )" },
                  "PlaceholderOnly": { "target": "Package", "version": "[1.0.0, )" },
                  "MetaPackage": { "target": "Package", "version": "[1.0.0, )" },
                  "Microsoft.NETCore.App": { "target": "Package", "version": "[10.0.0, )", "autoReferenced": true }
                }
              }
            }
          }
        }
        """);

        var assets = PackageAssetsReader.TryRead(projectPath);

        Assert.NotNull(assets);
        Assert.Equal(
            new[] { "coverlet.collector", "MetaPackage", "Newtonsoft.Json", "PlaceholderOnly" },
            assets.DeclaredPackageIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase));

        Assert.True(assets.AssemblyNamesByPackageId.ContainsKey("Newtonsoft.Json"));
        Assert.Contains("Newtonsoft.Json", assets.AssemblyNamesByPackageId["Newtonsoft.Json"]);

        // Packages that contribute no compile/runtime assembly cannot have "no detected symbol
        // usage" held against them: analyzers, build assets, placeholders, meta-packages.
        Assert.Equal(
            new[] { "coverlet.collector", "MetaPackage", "PlaceholderOnly" },
            assets.BuildOnlyPackageIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase));
    }

    private void WriteAssetsFile(string content)
    {
        File.WriteAllText(Path.Combine(projectDirectory, "obj", "project.assets.json"), content);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
