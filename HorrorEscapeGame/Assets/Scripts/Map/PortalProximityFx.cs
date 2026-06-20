using UnityEngine;

public class PortalProximityFx : MonoBehaviour
{
    [SerializeField] private Light portalLight;
    [SerializeField] private AudioSource humSource;
    [SerializeField] private ParticleSystem particles;

    private Transform _player;
    private float _baseLightIntensity;
    private float _pulsePhase;

    private const float ProximityRadius = 9f;
    private const float NearRadius = 2.8f;

    public void Configure(Light light, AudioSource hum, ParticleSystem ps)
    {
        portalLight = light;
        humSource = hum;
        particles = ps;
        if (portalLight != null)
            _baseLightIntensity = portalLight.intensity;
    }

    private void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
            _player = playerGo.transform;
    }

    private void Update()
    {
        if (_player == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                _player = playerGo.transform;
            if (_player == null) return;
        }

        float distance = Vector3.Distance(_player.position, transform.position);
        float proximity = 1f - Mathf.Clamp01((distance - NearRadius) / (ProximityRadius - NearRadius));

        _pulsePhase += Time.deltaTime * (1.6f + proximity * 2.4f);
        float pulse = 0.82f + Mathf.Sin(_pulsePhase) * 0.18f;

        if (portalLight != null)
            portalLight.intensity = Mathf.Lerp(_baseLightIntensity * 0.35f, _baseLightIntensity * 1.35f, proximity) * pulse;

        if (humSource != null && humSource.clip != null)
            humSource.volume = Mathf.Lerp(0f, 0.55f, proximity * proximity);

        if (particles != null)
        {
            var emission = particles.emission;
            emission.rateOverTime = Mathf.Lerp(6f, 22f, proximity);
        }
    }
}
