using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Metrics.Tests;

public sealed class SimplicitySnapshotTests
{
    private static readonly string[] MeasuredPropertyNames =
    [
        "TotalProjects",
        "TotalFiles",
        "PrimaryPathFileCount",
        "AbstractionLayerCount",
        "ExternalDependencyCount",
        "UnusedDependencyCount",
        "InterfacesWithSingleImplementation",
        "AverageMethodComplexity",
        "EstimatedOnboardingTime",
        "CollectedAt"
    ];

    [Fact]
    public void PublicContract_UsesRequiredInitProperties_NotAPositionalConstructor()
    {
        var snapshotType = typeof(SimplicitySnapshot);

        Assert.True(snapshotType.IsSealed);

        // The 0.4.x positional 10-parameter constructor is gone; the only public constructor is
        // the parameterless one, and every measured value is a required init property.
        var publicConstructors = snapshotType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        var constructor = Assert.Single(publicConstructors);
        Assert.Empty(constructor.GetParameters());

        var publicProperties = snapshotType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .ToDictionary(property => property.Name, StringComparer.Ordinal);

        Assert.Equal(
            [
                "AbstractionLayerCount",
                "AverageMethodComplexity",
                "CollectedAt",
                "EstimatedOnboardingTime",
                "ExternalDependencyCount",
                "InterfacesWithSingleImplementation",
                "PrematureAbstractionRatio",
                "PrimaryPathFileCount",
                "PrimaryPathRatio",
                "TotalFiles",
                "TotalProjects",
                "UnusedDependencyCount"
            ],
            publicProperties.Keys.OrderBy(name => name, StringComparer.Ordinal));

        AssertRequiredInitProperty<int>(publicProperties, "TotalProjects");
        AssertRequiredInitProperty<int>(publicProperties, "TotalFiles");
        AssertRequiredInitProperty<int>(publicProperties, "PrimaryPathFileCount");
        AssertRequiredInitProperty<int>(publicProperties, "AbstractionLayerCount");
        AssertRequiredInitProperty<int>(publicProperties, "ExternalDependencyCount");
        AssertRequiredInitProperty<int>(publicProperties, "UnusedDependencyCount");
        AssertRequiredInitProperty<int>(publicProperties, "InterfacesWithSingleImplementation");
        AssertRequiredInitProperty<double>(publicProperties, "AverageMethodComplexity");
        AssertRequiredInitProperty<TimeSpan?>(publicProperties, "EstimatedOnboardingTime");
        AssertRequiredInitProperty<DateTimeOffset>(publicProperties, "CollectedAt");
        AssertComputedProperty<double>(publicProperties, "PrimaryPathRatio");
        AssertComputedProperty<double>(publicProperties, "PrematureAbstractionRatio");

        var toSummary = snapshotType.GetMethod("ToSummary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotNull(toSummary);
        Assert.Empty(toSummary!.GetParameters());
        Assert.Equal(typeof(string), toSummary.ReturnType);
    }

    [Fact]
    public void PublicContract_EveryMeasuredPropertyIsRequired()
    {
        foreach (var propertyName in MeasuredPropertyNames)
        {
            var property = typeof(SimplicitySnapshot).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property);
            Assert.True(
                property!.GetCustomAttribute<RequiredMemberAttribute>() is not null,
                $"Expected '{propertyName}' to be a required member.");
        }
    }

    [Fact]
    public void DerivedRatios_FollowSpecAndGuardAgainstZero()
    {
        var populatedSnapshot = new SimplicitySnapshot
        {
            TotalProjects = 3,
            TotalFiles = 40,
            PrimaryPathFileCount = 28,
            AbstractionLayerCount = 8,
            ExternalDependencyCount = 9,
            UnusedDependencyCount = 2,
            InterfacesWithSingleImplementation = 2,
            AverageMethodComplexity = 3.0,
            EstimatedOnboardingTime = TimeSpan.FromHours(31),
            CollectedAt = new DateTimeOffset(2026, 4, 29, 12, 0, 0, TimeSpan.Zero)
        };

        Assert.Equal(0.7, populatedSnapshot.PrimaryPathRatio);
        Assert.Equal(0.25, populatedSnapshot.PrematureAbstractionRatio);

        var emptySnapshot = new SimplicitySnapshot
        {
            TotalProjects = 0,
            TotalFiles = 0,
            PrimaryPathFileCount = 0,
            AbstractionLayerCount = 0,
            ExternalDependencyCount = 0,
            UnusedDependencyCount = 0,
            InterfacesWithSingleImplementation = 0,
            AverageMethodComplexity = 0,
            EstimatedOnboardingTime = TimeSpan.Zero,
            CollectedAt = DateTimeOffset.UnixEpoch
        };

        Assert.Equal(0d, emptySnapshot.PrimaryPathRatio);
        Assert.Equal(0d, emptySnapshot.PrematureAbstractionRatio);
    }

    [Fact]
    public void ToSummary_MatchesSpecFormat_IndependentlyOfCurrentCulture()
    {
        using var _ = new CultureScope("fr-FR");

        var snapshot = new SimplicitySnapshot
        {
            TotalProjects = 3,
            TotalFiles = 40,
            PrimaryPathFileCount = 28,
            AbstractionLayerCount = 6,
            ExternalDependencyCount = 9,
            UnusedDependencyCount = 2,
            InterfacesWithSingleImplementation = 1,
            AverageMethodComplexity = 3.0,
            EstimatedOnboardingTime = TimeSpan.FromHours(31),
            CollectedAt = new DateTimeOffset(2026, 4, 29, 15, 45, 0, TimeSpan.FromHours(-4))
        };

        var expected = string.Join(
            Environment.NewLine,
            [
                "Simplicity Snapshot (2026-04-29)",
                "----------------------------------------",
                "Projects: 3",
                "Total files: 40",
                "Primary path files: 28",
                "Abstraction layers: 6",
                "Single-impl interfaces: 1",
                "External deps: 9 (2 unused)",
                "Avg complexity: 3.0",
                "Est. onboarding: 31h"
            ]);

        Assert.Equal(expected, snapshot.ToSummary());
    }

    [Fact]
    public void ToSummary_AnnouncesUncomputedOnboardingTime()
    {
        var snapshot = new SimplicitySnapshot
        {
            TotalProjects = 3,
            TotalFiles = 40,
            PrimaryPathFileCount = 28,
            AbstractionLayerCount = 6,
            ExternalDependencyCount = 9,
            UnusedDependencyCount = 2,
            InterfacesWithSingleImplementation = 1,
            AverageMethodComplexity = 3.0,
            EstimatedOnboardingTime = null,
            CollectedAt = new DateTimeOffset(2026, 4, 29, 15, 45, 0, TimeSpan.FromHours(-4))
        };

        Assert.EndsWith("Est. onboarding: not computed", snapshot.ToSummary(), StringComparison.Ordinal);
    }

    private static void AssertRequiredInitProperty<T>(
        IReadOnlyDictionary<string, PropertyInfo> publicProperties,
        string propertyName)
    {
        Assert.True(publicProperties.TryGetValue(propertyName, out var property), $"Expected public property '{propertyName}'.");
        Assert.Equal(typeof(T), property!.PropertyType);
        Assert.True(
            IsInitOnly(property),
            $"Expected '{propertyName}' to remain immutable via an init-only setter.");
        Assert.True(
            property.GetCustomAttribute<RequiredMemberAttribute>() is not null,
            $"Expected '{propertyName}' to be a required member.");
    }

    private static void AssertComputedProperty<T>(
        IReadOnlyDictionary<string, PropertyInfo> publicProperties,
        string propertyName)
    {
        Assert.True(publicProperties.TryGetValue(propertyName, out var property), $"Expected public property '{propertyName}'.");
        Assert.Equal(typeof(T), property!.PropertyType);
        Assert.Null(property.SetMethod);
    }

    private static bool IsInitOnly(PropertyInfo property)
    {
        return property.SetMethod is not null &&
               property.SetMethod.ReturnParameter
                   .GetRequiredCustomModifiers()
                   .Contains(typeof(IsExternalInit));
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo originalCulture;
        private readonly CultureInfo originalUICulture;

        public CultureScope(string cultureName)
        {
            originalCulture = CultureInfo.CurrentCulture;
            originalUICulture = CultureInfo.CurrentUICulture;

            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }
}
