using NAudio.Dsp;

public class BiQuadFilterAdapter : IFilter
{
    private readonly BiQuadFilter filter;

    public BiQuadFilterAdapter(BiQuadFilter filter)
    {
        this.filter = filter;
    }

    public float Transform(float sample) => filter.Transform(sample);
}
