using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class PlayerHuntedAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] huntClips;
    [SerializeField] private float volume = 1f;

    private static readonly Regex ClipNumberPattern = new(@"(\d+)\s*$", RegexOptions.Compiled);

    private AudioSource _source;
    private MonsterAI[] _monsters;
    private int _playlistIndex;
    private bool _playing;

    public void Initialize(AudioClip[] clips)
    {
        huntClips = SortChaseClips(clips);
        EnsureSource();

        if (huntClips == null) return;

        foreach (var clip in huntClips)
        {
            if (clip == null || clip.loadState == AudioDataLoadState.Loaded) continue;
            clip.LoadAudioData();
        }
    }

    private void Start()
    {
        EnsureSource();
        CacheMonsters();
    }

    private void CacheMonsters()
    {
        var monsters = GameObject.FindGameObjectsWithTag("Monster");
        _monsters = new MonsterAI[monsters.Length];
        for (int i = 0; i < monsters.Length; i++)
            _monsters[i] = monsters[i].GetComponent<MonsterAI>();
    }

    private bool IsAnyMonsterLockedOn()
    {
        if (_monsters == null) return false;

        foreach (var monster in _monsters)
        {
            if (monster != null && monster.IsLockedOn)
                return true;
        }

        return false;
    }

    private void LateUpdate()
    {
        if (_monsters == null || _monsters.Length == 0 || huntClips == null || huntClips.Length == 0)
            return;

        if (IsAnyMonsterLockedOn())
        {
            if (!_playing)
                BeginPlaylist();
            else if (!_source.isPlaying)
                AdvancePlaylist();
        }
        else if (_playing)
        {
            EndPlaylist();
        }
    }

    private void EnsureSource()
    {
        if (_source != null) return;

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;
        _source.volume = volume;
        _source.priority = 0;
    }

    private void BeginPlaylist()
    {
        EnsureSource();
        _playlistIndex = 0;
        PlayClipAt(_playlistIndex);
        _playing = true;
    }

    private void AdvancePlaylist()
    {
        _playlistIndex = (_playlistIndex + 1) % huntClips.Length;
        PlayClipAt(_playlistIndex);
    }

    private void PlayClipAt(int index)
    {
        var clip = huntClips[index];
        if (clip == null) return;

        if (clip.loadState != AudioDataLoadState.Loaded)
            clip.LoadAudioData();

        _source.clip = clip;
        _source.volume = volume;
        _source.loop = false;
        _source.Play();
    }

    private void EndPlaylist()
    {
        _source.Stop();
        _source.clip = null;
        _playlistIndex = 0;
        _playing = false;
    }

    private static AudioClip[] SortChaseClips(AudioClip[] clips)
    {
        if (clips == null || clips.Length <= 1)
            return clips;

        var sorted = (AudioClip[])clips.Clone();
        Array.Sort(sorted, (a, b) => GetClipNumber(a).CompareTo(GetClipNumber(b)));
        return sorted;
    }

    private static int GetClipNumber(AudioClip clip)
    {
        if (clip == null) return int.MaxValue;

        var match = ClipNumberPattern.Match(clip.name);
        return match.Success && int.TryParse(match.Groups[1].Value, out int n) ? n : int.MaxValue;
    }

    private void OnDisable() => EndPlaylist();
}
