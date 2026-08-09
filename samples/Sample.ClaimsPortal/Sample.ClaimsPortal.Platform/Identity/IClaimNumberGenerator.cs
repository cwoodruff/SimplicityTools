namespace Sample.ClaimsPortal.Platform.Identity;

/// <summary>
/// SF0001 hit: one implementation, <see cref="SequentialClaimNumberGenerator" />.
/// </summary>
public interface IClaimNumberGenerator
{
    string Next(string prefix);
}
