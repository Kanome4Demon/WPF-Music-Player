public class MultiChannelEqualizer : IEqualizer
{
    private readonly IFilter[][] filters;
    private readonly int channels;

    public MultiChannelEqualizer(int channels, Func<int, IFilter[]> filterFactory)
    {
        this.channels = channels;
        filters = new IFilter[channels][];
        for (int ch = 0; ch < channels; ch++)
        {
            filters[ch] = filterFactory(ch);
        }
    }

    public float ProcessSample(float sample, int channel)
    {
        foreach (var filter in filters[channel])
        {
            sample = filter.Transform(sample);
        }

        // 限制在 [-1,1]
        return Math.Clamp(sample, -1f, 1f);
    }
}
