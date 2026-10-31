namespace Northernlight.Core.Resources;

/// <summary>
///     Represents an abstract instance of a pipeline.
/// </summary>
public interface IPipeline
{
    /// <summary>
    ///     (Re)creates this pipeline.
    /// </summary>
    public void Recreate();
}
