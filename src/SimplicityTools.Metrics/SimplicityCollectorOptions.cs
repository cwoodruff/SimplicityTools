namespace SimplicityTools.Metrics;

/// <summary>
/// Opt-outs for the process-global side effects <see cref="SimplicityCollector" /> performs by
/// default. Library hosts that manage their own MSBuild environment should construct the
/// collector with these options instead of relying on the defaults.
/// </summary>
public sealed record SimplicityCollectorOptions
{
    /// <summary>
    /// Gets a value indicating whether the collector may spawn a <c>dotnet restore</c> child
    /// process for the target solution when package assets are missing or stale.
    /// <para>
    /// <b>Process-global effect:</b> when true (the default), collection can launch an external
    /// <c>dotnet</c> process that writes to the solution's <c>obj/</c> directories and the NuGet
    /// caches. Set to false to guarantee the collector never spawns a restore. With restore
    /// disabled, stale or missing <c>project.assets.json</c> files degrade the unused-dependency
    /// metrics: the unused-dependency pass already skips projects whose assets are missing, so
    /// <see cref="SimplicitySnapshot.UnusedDependencyCount" /> may undercount.
    /// </para>
    /// </summary>
    public bool AllowRestore { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the collector registers a default MSBuild instance via
    /// <c>Microsoft.Build.Locator</c> before opening the workspace.
    /// <para>
    /// <b>Process-global effect:</b> when true (the default), the collector calls
    /// <c>MSBuildLocator.RegisterDefaults()</c> on first use, which mutates assembly resolution
    /// for the entire process and can only ever bind one MSBuild instance per process. Set to
    /// false when the host process has already registered an MSBuild instance (or loaded MSBuild
    /// assemblies by other means); the host is then responsible for having done so, and
    /// collection fails if no MSBuild is available.
    /// </para>
    /// </summary>
    public bool RegisterMSBuildLocator { get; init; } = true;
}
