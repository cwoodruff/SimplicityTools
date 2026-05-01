using System.Diagnostics;
using Xunit;

namespace Sample.Simplified.Tests.EndToEnd;

public sealed class StartupSmokeTests
{
    [Fact]
    public async Task DotNetRun_StartsTheSampleSuccessfully()
    {
        var sampleRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var projectPath = Path.Combine(sampleRoot, "Sample.Simplified.App", "Sample.Simplified.App.csproj");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = sampleRoot
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Debug");
        startInfo.ArgumentList.Add("--no-build");

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await process.WaitForExitAsync(timeout.Token);

        var standardOutput = await process.StandardOutput.ReadToEndAsync(timeout.Token);
        var standardError = await process.StandardError.ReadToEndAsync(timeout.Token);

        Assert.Equal(0, process.ExitCode);
        Assert.Contains("Contoso Coffee placed 2 line(s) totaling 45.75.", standardOutput);
        Assert.DoesNotContain("Killed", standardError, StringComparison.OrdinalIgnoreCase);
    }
}
