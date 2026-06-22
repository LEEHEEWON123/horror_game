using UnityEngine;

public static class MouseSensitivityPrefs
{
    private const string Key = "MouseSensitivityV2";
    public const float Min = 0.8f;
    public const float Max = 8f;
    public const float Default = FirstPersonCamera.DefaultMouseSensitivity;

    public static float Load() =>
        PlayerPrefs.GetFloat(Key, Default);

    public static void Save(float value)
    {
        float clamped = Mathf.Clamp(value, Min, Max);
        PlayerPrefs.SetFloat(Key, clamped);
        PlayerPrefs.Save();
        SensitivityChanged?.Invoke(clamped);
    }

    public static event System.Action<float> SensitivityChanged;
}
