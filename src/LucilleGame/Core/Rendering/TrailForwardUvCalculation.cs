namespace Northernlight.Core.Rendering;

/// <summary>
///     A type of calculation to perform for the forward UVs
///     of a trail.
/// </summary>
public enum TrailForwardUvCalculation
{
    /// <summary>
    ///     The normal calculation, simply using trailProgress
    ///     for the U component.
    /// </summary>
    Normal,

    /// <summary>
    ///     Similar to Normal, but accounting for
    ///     accumulated arc length distance for the
    ///     U component.
    /// </summary>
    ArcLength
}
