using NAudio.Wave;

public class EqualizerSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly IEqualizer equalizer;
    private readonly int channels;

    public EqualizerSampleProvider(ISampleProvider source, IEqualizer equalizer)
    {
        this.source = source;
        this.equalizer = equalizer;
        channels = source.WaveFormat.Channels;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = source.Read(buffer, offset, count);

        for (int i = 0; i < read; i += channels)
        {
            for (int ch = 0; ch < channels; ch++)
            {
                int idx = offset + i + ch;
                buffer[idx] = equalizer.ProcessSample(buffer[idx], ch);
            }
        }

        return read;
    }

    public WaveFormat WaveFormat => source.WaveFormat;
}
