using NUnit.Framework;
using UnityEngine;

public class BackroomsAmbientAudioTests
{
    // ── FluorescentHumSynth ──────────────────────────────────────────────────

    [Test]
    public void HumSynth_FirstSample_IsZero()
    {
        var samples = new float[1024];
        FluorescentHumSynth.Fill(samples, frequency: 60f, sampleRate: 44100f);
        Assert.AreEqual(0f, samples[0], delta: 0.001f);
    }

    [Test]
    public void HumSynth_QuarterCycle_IsPeak()
    {
        float sampleRate = 44100f;
        float frequency = 60f;
        int quarterCycleSamples = Mathf.RoundToInt(sampleRate / frequency / 4f);

        var samples = new float[quarterCycleSamples + 1];
        FluorescentHumSynth.Fill(samples, frequency, sampleRate);

        Assert.AreEqual(1f, samples[quarterCycleSamples], delta: 0.01f);
    }

    [Test]
    public void HumSynth_HalfCycle_ReturnsToZero()
    {
        float sampleRate = 44100f;
        float frequency = 60f;
        int halfCycleSamples = Mathf.RoundToInt(sampleRate / frequency / 2f);

        var samples = new float[halfCycleSamples + 1];
        FluorescentHumSynth.Fill(samples, frequency, sampleRate);

        Assert.AreEqual(0f, samples[halfCycleSamples], delta: 0.01f);
    }

    // ── AmbientAudioVolume ───────────────────────────────────────────────────

    [Test]
    public void Volume_AtLightSource_IsOne()
    {
        float volume = AmbientAudioVolume.Calculate(distance: 0f, maxDistance: 10f);
        Assert.AreEqual(1f, volume, delta: 0.001f);
    }

    [Test]
    public void Volume_AtMaxDistance_IsBaseLevel()
    {
        float volume = AmbientAudioVolume.Calculate(distance: 10f, maxDistance: 10f);
        Assert.AreEqual(AmbientAudioVolume.BaseVolume, volume, delta: 0.001f);
    }

    [Test]
    public void Volume_BeyondMaxDistance_ClampsToBaseLevel()
    {
        float volume = AmbientAudioVolume.Calculate(distance: 999f, maxDistance: 10f);
        Assert.AreEqual(AmbientAudioVolume.BaseVolume, volume, delta: 0.001f);
    }

    [Test]
    public void Volume_AtHalfDistance_IsBetweenBaseAndOne()
    {
        float volume = AmbientAudioVolume.Calculate(distance: 5f, maxDistance: 10f);
        Assert.Greater(volume, AmbientAudioVolume.BaseVolume);
        Assert.Less(volume, 1f);
    }
}
