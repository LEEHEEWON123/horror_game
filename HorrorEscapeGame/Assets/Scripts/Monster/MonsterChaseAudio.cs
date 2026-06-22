using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MonsterChaseAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] chaseClips;
    [SerializeField] private float volume = 1f;

    private AudioSource _source;
    private bool _playing;

    public void Initialize(AudioClip[] clips)
    {
        chaseClips = clips;
        EnsureSource();

        if (chaseClips == null) return;

        foreach (var clip in chaseClips)
        {
            if (clip == null || clip.loadState == AudioDataLoadState.Loaded) continue;
            clip.LoadAudioData();
        }
    }

    public void BeginChase()
    {
        if (_playing || chaseClips == null || chaseClips.Length == 0)
            return;

        EnsureSource();

        var clip = chaseClips[Random.Range(0, chaseClips.Length)];
        if (clip == null) return;

        if (clip.loadState != AudioDataLoadState.Loaded)
            clip.LoadAudioData();

        _source.clip = clip;
        _source.volume = volume;
        _source.loop = true;
        _source.Play();
        _playing = true;
    }

    public void EndChase()
    {
        if (!_playing || _source == null) return;

        _source.loop = false;
        _source.Stop();
        _source.clip = null;
        _playing = false;
    }

    private void EnsureSource()
    {
        if (_source != null) return;
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = true;
        _source.spatialBlend = 0f;
        _source.volume = volume;
        _source.priority = 0;
    }

    private void OnDisable() => EndChase();
}
