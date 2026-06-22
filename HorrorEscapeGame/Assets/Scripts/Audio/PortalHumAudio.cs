using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PortalHumAudio : MonoBehaviour
{
    private AudioSource _source;

    public AudioSource Source
    {
        get
        {
            if (_source == null)
                _source = GetComponent<AudioSource>();
            return _source;
        }
    }

    public void Configure(AudioClip clip)
    {
        var source = Source;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.minDistance = 2f;
        source.maxDistance = 18f;
        source.volume = 0f;

        if (clip == null) return;

        source.clip = clip;
        source.Play();
    }
}
