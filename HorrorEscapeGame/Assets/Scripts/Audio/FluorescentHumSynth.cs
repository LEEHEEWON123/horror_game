using System;
using UnityEngine;

/// <summary>
/// Generates a fluorescent-light hum sine wave into a sample buffer.
/// Pure static logic — no MonoBehaviour dependency.
/// </summary>
public static class FluorescentHumSynth
{
    /// <summary>
    /// Fills <paramref name="samples"/> with a sine wave starting at phase 0.
    /// </summary>
    /// <param name="samples">Output buffer (mono).</param>
    /// <param name="frequency">Hum frequency in Hz (e.g. 60).</param>
    /// <param name="sampleRate">Audio sample rate in Hz (e.g. 44100).</param>
    public static void Fill(float[] samples, float frequency, float sampleRate)
    {
        float phaseIncrement = 2f * Mathf.PI * frequency / sampleRate;
        for (int i = 0; i < samples.Length; i++)
            samples[i] = Mathf.Sin(phaseIncrement * i);
    }

    /// <summary>
    /// Creates a loopable AudioClip containing one full cycle of the hum.
    /// </summary>
    public static AudioClip CreateClip(float frequency = 60f, float durationSeconds = 1f, int sampleRate = 44100)
    {
        int sampleCount = Mathf.RoundToInt(sampleRate * durationSeconds);
        var samples = new float[sampleCount];
        Fill(samples, frequency, sampleRate);

        var clip = AudioClip.Create("FluorescentHum", sampleCount, channels: 1, sampleRate, stream: false);
        clip.SetData(samples, offsetSamples: 0);
        return clip;
    }
}
