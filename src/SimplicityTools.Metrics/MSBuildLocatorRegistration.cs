using Microsoft.Build.Locator;

namespace SimplicityTools.Metrics;

internal static class MSBuildLocatorRegistration
{
    private static readonly object SyncLock = new();
    private static bool initialized;

    public static void EnsureRegistered()
    {
        lock (SyncLock)
        {
            if (initialized || MSBuildLocator.IsRegistered)
            {
                initialized = true;
                return;
            }

            // Only latch after a successful registration so a failed attempt can be retried.
            MSBuildLocator.RegisterDefaults();
            initialized = true;
        }
    }
}
