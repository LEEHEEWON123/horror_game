using UnityEngine;

/// <summary>
/// Calculates ambient hum volume based on distance to the nearest light source.
/// Pure static logic — no MonoBehaviour dependency.
/// </summary>
public static class AmbientAudioVolume
{
    /// <summary>Minimum volume regardless of distance (hum is never silent).</summary>
    public const float BaseVolume = 0.3f;

    /// <summary>
    /// Returns a volume in [BaseVolume, 1.0] based on how close the listener is
    /// to a light source.
    /// </summary>
    /// <param name="distance">Distance to the nearest light (metres).</param>
    /// <param name="maxDistance">Distance at which volume drops to BaseVolume.</param>
    public static float Calculate(float distance, float maxDistance)
    {
        if (maxDistance <= 0f) return BaseVolume;
        float t = Mathf.Clamp01(distance / maxDistance);
        return Mathf.Lerp(1f, BaseVolume, t);
    }
}
