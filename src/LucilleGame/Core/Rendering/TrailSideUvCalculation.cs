namespace Northernlight.Core.Rendering;

/// <summary>
///     A type of calculation to perform for the side UVs
///     of a trail.
/// </summary>
public enum TrailSideUvCalculation
{
    /// <summary>
    ///     The normal calculation, simply using (trailProgress, 0/1) 
    ///     for the V component.
    /// </summary>
    Normal,

    // Exists mostly as legacy. This is quite ugly.
    /// <summary>
    ///     A nonstandard hack, using (trailProgress, 0.5 +- width * 0.5f)
    ///     for the V component.
    /// </summary>
    /// <remarks>
    ///     The intention with this is that it
    ///     allows the width to be calculated
    ///     on a per-fragment basis in a pixel
    ///     shader by undoing this UV transformation
    ///     in said pixel shader, thus achieving
    ///     extreme width granularity.
    /// </remarks>
    PixelShaderWidthHack
}
