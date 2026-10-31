namespace Northernlight.Core.Utilities;

/// <summary>
///     A simple collection of easing functions.
/// </summary>
public static class Easings
{
    extension(float)
    {
        /// <summary>
        ///     An in-out back easing function.
        /// </summary>
        public static float EaseInOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;

            float twoX = x * 2f;
            float leftPiece = twoX.Squared() * ((c2 + 1f) * twoX - c2) * 0.5f;
            float rightPiece = ((twoX - 2f).Squared() * ((c2 + 1f) * (twoX - 2f) + c2) + 2f) * 0.5f;

            return x < 0.5f ? leftPiece : rightPiece;
        }
    }
}
