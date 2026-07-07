using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Metrics.Tests;

public sealed class SimplicitySnapshotTests
{
    private static readonly Type[] ExpectedConstructorTypes =
    [
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(double),
        typeof(TimeSpan?),
        typeof(DateTimeOffset)
    ];

    [Fact]
    public void PublicContract_HasExpectedConstructorAndMembers()
    {
        var snapshotType = typeof(SimplicitySnapshot);

        Assert.True(snapshotType.IsSealed);

        var constructor = snapshotType.GetConstructor(ExpectedConstructorTypes);
        Assert.NotNull(constructor);

        Assert.Collection(
            constructor!.GetParameters(),
            parameter => AssertParameter(parameter, "TotalProjects", typeof(int)),
            parameter => AssertParameter(parameter, "TotalFiles", typeof(int)),
            parameter => AssertParameter(parameter, "PrimaryPathFileCount", typeof(int)),
            parameter => AssertParameter(parameter, "AbstractionLayerCount", typeof(int)),
            parameter => AssertParameter(parameter, "ExternalDependencyCount", typeof(int)),
            parameter => AssertParameter(parameter, "UnusedDependencyCount", typeof(int)),
            parameter => AssertParameter(parameter, "InterfacesWithSingleImplementation", typeof(int)),
            parameter => AssertParameter(parameter, "AverageMethodComplexity", typeof(double)),
            parameter => AssertParameter(parameter, "EstimatedOnboardingTime", typeof(TimeSpan?)),
            parameter => AssertParameter(parameter, "CollectedAt", typeof(DateTimeOffset)));

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

        AssertProperty<int>(publicProperties, "TotalProjects", initOnly: true);
        AssertProperty<int>(publicProperties, "TotalFiles", initOnly: true);
        AssertProperty<int>(publicProperties, "PrimaryPathFileCount", initOnly: true);
        AssertProperty<int>(publicProperties, "AbstractionLayerCount", initOnly: true);
        AssertProperty<int>(publicProperties, "ExternalDependencyCount", initOnly: true);
        AssertProperty<int>(publicProperties, "UnusedDependencyCount", initOnly: true);
        AssertProperty<int>(publicProperties, "InterfacesWithSingleImplementation", initOnly: true);
        AssertProperty<double>(publicProperties, "AverageMethodComplexity", initOnly: true);
        AssertProperty<TimeSpan?>(publicProperties, "EstimatedOnboardingTime", initOnly: true);
        AssertProperty<DateTimeOffset>(publicProperties, "CollectedAt", initOnly: true);
        AssertProperty<double>(publicProperties, "PrimaryPathRatio", initOnly: false);
        AssertProperty<double>(publicProperties, "PrematureAbstractionRatio", initOnly: false);

        var toSummary = snapshotType.GetMethod("ToSummary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotNull(toSummary);
        Assert.Empty(toSummary!.GetParameters());
        Assert.Equal(typeof(string), toSummary.ReturnType);
    }

    [Fact]
    public void DerivedRatios_FollowSpecAndGuardAgainstZero()
    {
        var populatedSnapshot = CreateSnapshot(
            totalProjects: 3,
            totalFiles: 40,
            primaryPathFileCount: 28,
            abstractionLayerCount: 8,
            externalDependencyCount: 9,
            unusedDependencyCount: 2,
            interfacesWithSingleImplementation: 2,
            averageMethodComplexity: 3.0,
            estimatedOnboardingTime: TimeSpan.FromHours(31),
            collectedAt: new DateTimeOffset(2026, 4, 29, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(0.7, GetProperty<double>(populatedSnapshot, "PrimaryPathRatio"));
        Assert.Equal(0.25, GetProperty<double>(populatedSnapshot, "PrematureAbstractionRatio"));

        var emptySnapshot = CreateSnapshot(
            totalProjects: 0,
            totalFiles: 0,
            primaryPathFileCount: 0,
            abstractionLayerCount: 0,
            externalDependencyCount: 0,
            unusedDependencyCount: 0,
            interfacesWithSingleImplementation: 0,
            averageMethodComplexity: 0,
            estimatedOnboardingTime: TimeSpan.Zero,
            collectedAt: DateTimeOffset.UnixEpoch);

        Assert.Equal(0d, GetProperty<double>(emptySnapshot, "PrimaryPathRatio"));
        Assert.Equal(0d, GetProperty<double>(emptySnapshot, "PrematureAbstractionRatio"));
    }

    [Fact]
    public void ToSummary_MatchesSpecFormat_IndependentlyOfCurrentCulture()
    {
        using var _ = new CultureScope("fr-FR");

        var snapshot = CreateSnapshot(
            totalProjects: 3,
            totalFiles: 40,
            primaryPathFileCount: 28,
            abstractionLayerCount: 6,
            externalDependencyCount: 9,
            unusedDependencyCount: 2,
            interfacesWithSingleImplementation: 1,
            averageMethodComplexity: 3.0,
            estimatedOnboardingTime: TimeSpan.FromHours(31),
            collectedAt: new DateTimeOffset(2026, 4, 29, 15, 45, 0, TimeSpan.FromHours(-4)));

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

        Assert.Equal(expected, InvokeToSummary(snapshot));
    }

    [Fact]
    public void ToSummary_AnnouncesUncomputedOnboardingTime()
    {
        var snapshot = CreateSnapshot(
            totalProjects: 3,
            totalFiles: 40,
            primaryPathFileCount: 28,
            abstractionLayerCount: 6,
            externalDependencyCount: 9,
            unusedDependencyCount: 2,
            interfacesWithSingleImplementation: 1,
            averageMethodComplexity: 3.0,
            estimatedOnboardingTime: null,
            collectedAt: new DateTimeOffset(2026, 4, 29, 15, 45, 0, TimeSpan.FromHours(-4)));

        Assert.EndsWith("Est. onboarding: not computed", InvokeToSummary(snapshot), StringComparison.Ordinal);
    }

    private static void AssertParameter(ParameterInfo parameter, string expectedName, Type expectedType)
    {
        Assert.Equal(expectedName, parameter.Name);
        Assert.Equal(expectedType, parameter.ParameterType);
    }

    private static void AssertProperty<T>(
        IReadOnlyDictionary<string, PropertyInfo> publicProperties,
        string propertyName,
        bool initOnly)
    {
        Assert.True(publicProperties.TryGetValue(propertyName, out var property), $"Expected public property '{propertyName}'.");
        Assert.Equal(typeof(T), property!.PropertyType);

        if (initOnly)
        {
            Assert.True(
                IsInitOnly(property),
                $"Expected '{propertyName}' to remain immutable via an init-only setter.");
            return;
        }

        Assert.Null(property.SetMethod);
    }

    private static object CreateSnapshot(
        int totalProjects,
        int totalFiles,
        int primaryPathFileCount,
        int abstractionLayerCount,
        int externalDependencyCount,
        int unusedDependencyCount,
        int interfacesWithSingleImplementation,
        double averageMethodComplexity,
        TimeSpan? estimatedOnboardingTime,
        DateTimeOffset collectedAt)
    {
        var constructor = typeof(SimplicitySnapshot).GetConstructor(ExpectedConstructorTypes);
        Assert.NotNull(constructor);

        return constructor!.Invoke(
        [
            totalProjects,
            totalFiles,
            primaryPathFileCount,
            abstractionLayerCount,
            externalDependencyCount,
            unusedDependencyCount,
            interfacesWithSingleImplementation,
            averageMethodComplexity,
            estimatedOnboardingTime!,
            collectedAt
        ]);
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = typeof(SimplicitySnapshot).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);

        return Assert.IsType<T>(property!.GetValue(instance));
    }

    private static string InvokeToSummary(object snapshot)
    {
        var toSummary = typeof(SimplicitySnapshot).GetMethod("ToSummary", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(toSummary);

        return Assert.IsType<string>(toSummary!.Invoke(snapshot, null));
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
