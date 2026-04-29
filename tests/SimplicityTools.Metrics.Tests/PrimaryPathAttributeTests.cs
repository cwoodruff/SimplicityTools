using System.Reflection;
using SimplicityTools.Metrics;
using Xunit;

namespace SimplicityTools.Metrics.Tests;

public sealed class PrimaryPathAttributeTests
{
    [Fact]
    public void PublicContract_MatchesSpecification()
    {
        var attributeType = typeof(PrimaryPathAttribute);

        Assert.True(attributeType.IsPublic);
        Assert.True(attributeType.IsSealed);
        Assert.Equal(typeof(Attribute), attributeType.BaseType);

        var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, usage!.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
    }
}
