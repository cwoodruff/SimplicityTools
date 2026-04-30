using System.Threading;
using Microsoft.Build.Locator;

namespace SimplicityTools.Metrics;

internal static class MSBuildLocatorRegistration
{
    private static int initialized;

    public static void EnsureRegistered()
    {
        if (Interlocked.Exchange(ref initialized, 1) == 1 || MSBuildLocator.IsRegistered)
        {
            return;
        }

        MSBuildLocator.RegisterDefaults();
    }
}
