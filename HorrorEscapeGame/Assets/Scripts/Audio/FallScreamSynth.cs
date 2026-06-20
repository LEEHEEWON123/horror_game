using UnityEngine;

/// <summary>
/// 차원 이동 시 재생되는 비명 효과음 생성기.
/// 높은 음에서 낮은 음으로 내려가며 서서히 사라지는 사인파 sweep.
/// </summary>
public static class FallScreamSynth
{
    /// <summary>
    /// samples 버퍼를 내려가는 주파수 sweep + 진폭 감쇠로 채운다.
    /// </summary>
    public static void Fill(float[] samples, float startFreq, float endFreq, float sampleRate)
    {
        int n = samples.Length;
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float amplitude = 1f - t; // 시작 1.0 → 끝 0.0

            phase += 2f * Mathf.PI * freq / sampleRate;
            samples[i] = Mathf.Sin(phase) * amplitude;
        }
    }

    public static AudioClip CreateClip(float startFreq = 600f, float endFreq = 80f,
                                        float duration = 0.9f, int sampleRate = 44100)
    {
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        Fill(samples, startFreq, endFreq, sampleRate);

        var clip = AudioClip.Create("FallScream", sampleCount, channels: 1, sampleRate, stream: false);
        clip.SetData(samples, offsetSamples: 0);
        return clip;
    }
}
