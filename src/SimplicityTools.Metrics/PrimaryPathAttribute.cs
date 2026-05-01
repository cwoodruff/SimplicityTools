namespace SimplicityTools.Metrics;

/// <summary>
/// Marks a class or method as belonging to the primary business path for heuristic collection.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PrimaryPathAttribute : Attribute
{
}
