#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
#endif
using UnityEngine;

public static class EntityChaseAudioSetup
{
    private const string SfxFolder = "Assets/Backrooms Entity SFX";
    private static AudioClip[] _cachedClips;

#if UNITY_EDITOR
    private static readonly Regex ClipNumberPattern = new(@"(\d+)\s*$", RegexOptions.Compiled);
#endif

    public static AudioClip[] LoadChaseClips()
    {
        if (_cachedClips != null && _cachedClips.Length > 0)
            return _cachedClips;

#if UNITY_EDITOR
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder });
        var clips = new List<AudioClip>(guids.Length);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                clips.Add(clip);
        }

        if (clips.Count == 0)
        {
            var fallback = AssetDatabase.LoadAssetAtPath<AudioClip>(
                $"{SfxFolder}/juanjo_sound - Backrooms Entity 1.wav");
            if (fallback != null)
                clips.Add(fallback);
        }

        clips.Sort((a, b) => GetClipNumber(a).CompareTo(GetClipNumber(b)));
        _cachedClips = clips.ToArray();
        return _cachedClips;
#else
        return System.Array.Empty<AudioClip>();
#endif
    }

#if UNITY_EDITOR
    private static int GetClipNumber(AudioClip clip)
    {
        if (clip == null) return int.MaxValue;

        var match = ClipNumberPattern.Match(clip.name);
        return match.Success && int.TryParse(match.Groups[1].Value, out int n) ? n : int.MaxValue;
    }
#endif

    public static MonsterChaseAudio Apply(GameObject entity, AudioClip[] clips)
    {
        if (entity == null) return null;

        if (clips == null || clips.Length == 0)
            clips = LoadChaseClips();

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("[EntityChaseAudio] No chase clips loaded.");
            return null;
        }

        var audio = entity.GetComponent<MonsterChaseAudio>();
        if (audio == null)
            audio = entity.AddComponent<MonsterChaseAudio>();

        audio.Initialize(clips);
        return audio;
    }
}
