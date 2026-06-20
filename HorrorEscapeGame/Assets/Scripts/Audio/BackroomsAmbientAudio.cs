using UnityEngine;

/// <summary>
/// Plays a procedurally generated fluorescent-light hum that gets louder
/// as the player approaches ceiling lights.
///
/// Usage: call BackroomsAmbientAudio.AddTo(gameObject) from BackroomsAtmosphere.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackroomsAmbientAudio : MonoBehaviour
{
    [SerializeField] private float humFrequency = 60f;
    [SerializeField] private float lightInfluenceRadius = 13f; // match ceiling light range
    [SerializeField] private float masterVolume = 0.04f;       // overall loudness cap

    private AudioSource _source;
    private Light[] _ceilingLights;
    private float _lightRefreshTimer;
    private const float LightRefreshInterval = 2f;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.clip = FluorescentHumSynth.CreateClip(humFrequency);
        _source.loop = true;
        _source.spatialBlend = 0f; // 2D — hum fills the whole room
        _source.volume = AmbientAudioVolume.BaseVolume * masterVolume;
        _source.priority = 64;
        _source.Play();
    }

    private void Update()
    {
        _lightRefreshTimer -= Time.deltaTime;
        if (_lightRefreshTimer <= 0f)
        {
            _ceilingLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            _lightRefreshTimer = LightRefreshInterval;
        }

        float dist = NearestLightDistance();
        _source.volume = AmbientAudioVolume.Calculate(dist, lightInfluenceRadius) * masterVolume;
    }

    private float NearestLightDistance()
    {
        if (_ceilingLights == null || _ceilingLights.Length == 0)
            return lightInfluenceRadius;

        float minDist = float.MaxValue;
        var cam = Camera.main;
        Vector3 pos = cam != null ? cam.transform.position : transform.position;

        foreach (var light in _ceilingLights)
        {
            if (light == null || light.type != LightType.Point) continue;
            float d = Vector3.Distance(pos, light.transform.position);
            if (d < minDist) minDist = d;
        }

        return minDist == float.MaxValue ? lightInfluenceRadius : minDist;
    }

    /// <summary>
    /// Attaches BackroomsAmbientAudio to a root GameObject and follows the player.
    /// </summary>
    public static BackroomsAmbientAudio AddTo(GameObject root)
    {
        var go = new GameObject("BackroomsAmbientAudio");
        go.transform.SetParent(root.transform, worldPositionStays: false);
        return go.AddComponent<BackroomsAmbientAudio>();
    }
}
