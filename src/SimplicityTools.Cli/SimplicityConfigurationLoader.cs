using System.Text.Json;

namespace SimplicityTools.Cli;

internal static class SimplicityConfigurationLoader
{
    private const string ConfigFileName = "simplicity.json";

    public static SimplicityConfiguration LoadForSolution(string solutionPath, TextWriter warningWriter, bool warnWhenMissing = true)
    {
        ArgumentNullException.ThrowIfNull(solutionPath);
        ArgumentNullException.ThrowIfNull(warningWriter);

        var solutionDirectory = GetSolutionDirectory(solutionPath);
        var configPath = GetPathForSolution(solutionPath);

        if (!File.Exists(configPath))
        {
            if (warnWhenMissing)
            {
                warningWriter.WriteLine($"Info: using built-in defaults (no {ConfigFileName} found in '{solutionDirectory}').");
            }

            return SimplicityConfiguration.Defaults;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            return ParseConfiguration(document.RootElement);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Invalid {ConfigFileName}: {exception.Message}", exception);
        }
    }

    internal static string GetPathForSolution(string solutionPath)
    {
        var solutionDirectory = GetSolutionDirectory(solutionPath);
        return Path.Combine(solutionDirectory, ConfigFileName);
    }

    private static SimplicityConfiguration ParseConfiguration(JsonElement root)
    {
        EnsureObject(root, "The root value");
        EnsureAllowedProperties(root, "$", new HashSet<string>(StringComparer.Ordinal) { "tca", "filters" });

        var configuration = SimplicityConfiguration.Defaults;
        var tca = configuration.Tca;
        var filters = configuration.Filters;

        if (root.TryGetProperty("tca", out var tcaElement))
        {
            EnsureObject(tcaElement, "$.tca");
            EnsureAllowedProperties(
                tcaElement,
                "$.tca",
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "teamSize",
                    "averageEngineerMonthlySalaryUsd",
                    "estimatedMonthlyIncidentCount",
                    "onCallHourlyRateUsd",
                    "attritionCoefficientPercent"
                });

            tca = tca with
            {
                TeamSize = GetOptionalInt32(tcaElement, "teamSize", "$.tca.teamSize", minimum: 1, tca.TeamSize),
                AverageEngineerMonthlySalaryUsd = GetOptionalDecimal(tcaElement, "averageEngineerMonthlySalaryUsd", "$.tca.averageEngineerMonthlySalaryUsd", minimum: 0m, tca.AverageEngineerMonthlySalaryUsd, inclusiveMinimum: false),
                EstimatedMonthlyIncidentCount = GetOptionalInt32(tcaElement, "estimatedMonthlyIncidentCount", "$.tca.estimatedMonthlyIncidentCount", minimum: 0, tca.EstimatedMonthlyIncidentCount),
                OnCallHourlyRateUsd = GetOptionalDecimal(tcaElement, "onCallHourlyRateUsd", "$.tca.onCallHourlyRateUsd", minimum: 0m, tca.OnCallHourlyRateUsd, inclusiveMinimum: false),
                AttritionCoefficientPercent = GetOptionalDecimal(tcaElement, "attritionCoefficientPercent", "$.tca.attritionCoefficientPercent", minimum: 0m, tca.AttritionCoefficientPercent, maximum: 100m)
            };
        }

        if (root.TryGetProperty("filters", out var filtersElement))
        {
            EnsureObject(filtersElement, "$.filters");
            EnsureAllowedProperties(
                filtersElement,
                "$.filters",
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "primaryPathRatioTarget",
                    "prematureAbstractionRatioTarget",
                    "maxMethodComplexity",
                    "maxOnboardingHours",
                    "passingScore"
                });

            filters = filters with
            {
                PrimaryPathRatioTarget = GetOptionalDouble(filtersElement, "primaryPathRatioTarget", "$.filters.primaryPathRatioTarget", minimum: 0d, filters.PrimaryPathRatioTarget, maximum: 1d),
                PrematureAbstractionRatioTarget = GetOptionalDouble(filtersElement, "prematureAbstractionRatioTarget", "$.filters.prematureAbstractionRatioTarget", minimum: 0d, filters.PrematureAbstractionRatioTarget, maximum: 1d),
                MaxMethodComplexity = GetOptionalDouble(filtersElement, "maxMethodComplexity", "$.filters.maxMethodComplexity", minimum: 0d, filters.MaxMethodComplexity, inclusiveMinimum: false),
                MaxOnboardingHours = GetOptionalDouble(filtersElement, "maxOnboardingHours", "$.filters.maxOnboardingHours", minimum: 0d, filters.MaxOnboardingHours, inclusiveMinimum: false),
                PassingScore = GetOptionalDouble(filtersElement, "passingScore", "$.filters.passingScore", minimum: 0d, filters.PassingScore, maximum: 1d)
            };
        }

        return configuration with
        {
            Tca = tca,
            Filters = filters
        };
    }

    private static string GetSolutionDirectory(string solutionPath)
    {
        var fullPath = Path.GetFullPath(solutionPath);
        return Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException($"Could not determine the directory for '{solutionPath}'.");
    }

    private static void EnsureObject(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"{path} must be a JSON object.");
        }
    }

    private static void EnsureAllowedProperties(JsonElement element, string path, IReadOnlySet<string> allowedProperties)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!allowedProperties.Contains(property.Name))
            {
                throw new JsonException($"{path} contains an unsupported property '{property.Name}'.");
            }
        }
    }

    private static int GetOptionalInt32(JsonElement parent, string propertyName, string path, int minimum, int defaultValue, int? maximum = null)
    {
        if (!parent.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (!property.TryGetInt32(out var value))
        {
            throw new JsonException($"{path} must be an integer.");
        }

        ValidateRange(value, path, minimum, maximum, inclusiveMinimum: true);
        return value;
    }

    private static decimal GetOptionalDecimal(JsonElement parent, string propertyName, string path, decimal minimum, decimal defaultValue, decimal? maximum = null, bool inclusiveMinimum = true)
    {
        if (!parent.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (!property.TryGetDecimal(out var value))
        {
            throw new JsonException($"{path} must be a number.");
        }

        ValidateRange(value, path, minimum, maximum, inclusiveMinimum);
        return value;
    }

    private static double GetOptionalDouble(JsonElement parent, string propertyName, string path, double minimum, double defaultValue, double? maximum = null, bool inclusiveMinimum = true)
    {
        if (!parent.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (!property.TryGetDouble(out var value))
        {
            throw new JsonException($"{path} must be a number.");
        }

        ValidateRange(value, path, minimum, maximum, inclusiveMinimum);
        return value;
    }

    private static void ValidateRange<T>(T value, string path, T minimum, T? maximum, bool inclusiveMinimum)
        where T : struct, IComparable<T>
    {
        var minimumComparison = value.CompareTo(minimum);
        if (inclusiveMinimum ? minimumComparison < 0 : minimumComparison <= 0)
        {
            var operatorText = inclusiveMinimum ? "greater than or equal to" : "greater than";
            throw new JsonException($"{path} must be {operatorText} {minimum}.");
        }

        if (maximum is { } upperBound && value.CompareTo(upperBound) > 0)
        {
            throw new JsonException($"{path} must be less than or equal to {upperBound}.");
        }
    }
}
