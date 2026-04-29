using System.Diagnostics;
using Microsoft.Build.Construction;

namespace SimplicityTools.Metrics;

internal static class SolutionRestoreCoordinator
{
    public static async Task RestoreIfNeededAsync(string solutionPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);

        var fullSolutionPath = Path.GetFullPath(solutionPath);
        if (!NeedsRestore(fullSolutionPath))
        {
            return;
        }

        var workingDirectory = Path.GetDirectoryName(fullSolutionPath)
            ?? throw new InvalidOperationException($"Could not resolve the directory for '{solutionPath}'.");
        var startInfo = new ProcessStartInfo("dotnet", $"restore \"{fullSolutionPath}\" --nologo")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start 'dotnet restore'.");
        using var registration = cancellationToken.Register(() =>
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        });

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"dotnet restore failed for '{solutionPath}'.{Environment.NewLine}{await standardOutput.ConfigureAwait(false)}{Environment.NewLine}{await standardError.ConfigureAwait(false)}");
    }

    private static bool NeedsRestore(string solutionPath)
    {
        var solution = SolutionFile.Parse(solutionPath);
        var solutionDirectory = Path.GetDirectoryName(solutionPath)
            ?? throw new InvalidOperationException($"Could not resolve the directory for '{solutionPath}'.");

        foreach (var project in solution.ProjectsInOrder)
        {
            if (project.ProjectType != SolutionProjectType.KnownToBeMSBuildFormat ||
                !string.Equals(Path.GetExtension(project.RelativePath), ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, project.RelativePath));
            if (!File.Exists(projectPath) || !ProjectDeclaresPackages(projectPath))
            {
                continue;
            }

            var assetsPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
            if (!File.Exists(assetsPath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ProjectDeclaresPackages(string projectPath)
    {
        var projectRoot = ProjectRootElement.Open(projectPath)
            ?? throw new InvalidOperationException($"Could not load project '{projectPath}'.");

        return projectRoot.Items.Any(item =>
            string.Equals(item.ItemType, "PackageReference", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(item.Include));
    }
}
