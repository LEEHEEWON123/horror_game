#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class PortalAudioSetup
{
    public const string SfxFolder = "Assets/Portal SFX";
    public const string HumClipPath = SfxFolder + "/portal_hum.wav";
    public const string EnterClipPath = SfxFolder + "/portal_enter.wav";

    public static AudioClip LoadHumLoop()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<AudioClip>(HumClipPath);
#else
        return null;
#endif
    }

    public static AudioClip LoadEnterClip()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<AudioClip>(EnterClipPath);
#else
        return null;
#endif
    }
}
