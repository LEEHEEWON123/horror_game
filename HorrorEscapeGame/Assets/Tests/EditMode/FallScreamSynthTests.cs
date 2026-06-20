using NUnit.Framework;

public class FallScreamSynthTests
{
    [Test]
    public void FirstSample_IsZero()
    {
        var samples = new float[4410];
        FallScreamSynth.Fill(samples, startFreq: 600f, endFreq: 80f, sampleRate: 44100f);
        Assert.AreEqual(0f, samples[0], delta: 0.001f);
    }

    [Test]
    public void LastSample_AmplitudeIsNearZero()
    {
        // 끝부분 진폭이 0에 가까워야 함 (envelope fade out)
        var samples = new float[44100];
        FallScreamSynth.Fill(samples, startFreq: 600f, endFreq: 80f, sampleRate: 44100f);
        Assert.AreEqual(0f, samples[44099], delta: 0.05f);
    }

    [Test]
    public void MidSamples_HaveNonZeroAmplitude()
    {
        var samples = new float[4410];
        FallScreamSynth.Fill(samples, startFreq: 600f, endFreq: 80f, sampleRate: 44100f);

        float maxMid = 0f;
        for (int i = 100; i < 1000; i++)
            if (samples[i] > maxMid) maxMid = samples[i];

        Assert.Greater(maxMid, 0.1f);
    }
}
