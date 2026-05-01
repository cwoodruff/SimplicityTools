namespace SimplicityTools.Filters;

/// <summary>
/// Identifies the supported Simplicity-First teaching filters.
/// </summary>
public enum FilterName
{
    /// <summary>
    /// Checks whether the system is understandable enough to debug at 2 AM.
    /// </summary>
    TwoAmTest,

    /// <summary>
    /// Checks whether abstraction and dependency growth stay below the Half-Rule threshold.
    /// </summary>
    HalfRule,

    /// <summary>
    /// Checks whether the primary business flow remains obvious and concentrated.
    /// </summary>
    PrimaryPathFirst
}
