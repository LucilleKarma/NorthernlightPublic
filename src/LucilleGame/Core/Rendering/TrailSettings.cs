using System.Numerics;
using Northernlight.Core.Resources;
using static Northernlight.Core.Resources.IMeshSettings;

namespace Northernlight.Core.Rendering;

/// <summary>
///     Settings that dictate how a given trail should be rendered.
/// </summary>
public sealed class TrailSettings<TTopologyData> : IMeshSettings where TTopologyData : ITrailTopologySetting
{
    public delegate float WidthFunctionDelegate(float trailProgress);

    public delegate Color ColorFunctionDelegate(float trailProgress);

    public required Shader VertexShader
    {
        get;
        set;
    }

    public required Shader FragmentShader
    {
        get;
        set;
    }

    public required PreparationActionDelegate PreparationAction
    {
        get;
        set;
    }

    public BlendState BlendState
    {
        get;
        set;
    } = BlendState.AlphaBlend;

    /// <summary>
    ///     The function which dictates the width of a trail
    ///     at a given 0-1 progress value.
    /// </summary>
    public required WidthFunctionDelegate WidthFunction
    {
        get;
        set;
    }

    /// <summary>
    ///     The function which dictates the color of a trail
    ///     at a given 0-1 progress value.
    /// </summary>
    public required ColorFunctionDelegate ColorFunction
    {
        get;
        set;
    }

    /// <summary>
    ///     The smoothing scheme to use on the control points.
    /// </summary>
    public StandardTrailSmoothingSchemes.SmoothingScheme Smoothing
    {
        get;
        set;
    } = StandardTrailSmoothingSchemes.HermiteInterpolation;

    /// <summary>
    ///     An optional override that may be used to specify the
    ///     amount of points on the trail.
    /// </summary>
    public int? PointCountOverride
    {
        get;
        set;
    }

    /// <summary>
    ///     The side UV calculation method to use for the trail.
    /// </summary>
    public TrailSideUvCalculation SideUvCalculationMethod
    {
        get;
        set;
    } = TrailSideUvCalculation.Normal;

    /// <summary>
    ///     The forward UV calculation method to use for the trail.
    /// </summary>
    public TrailForwardUvCalculation ForwardUvCalculationMethod
    {
        get;
        set;
    } = TrailForwardUvCalculation.Normal;

    /// <summary>
    ///     The suggested orthogonal vector to use when using cross
    ///     products to convert tangents to normals.
    /// </summary>
    public Vector3 SuggestedNormalOrthogonal
    {
        get;
        set;
    } = Vector3.UnitX;
}
