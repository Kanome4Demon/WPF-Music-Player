using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WPFMusicPlayerDemo.Audio;
/// <summary>
/// 改进后的均衡器，支持多声道
/// 低频和高频轻度增强
/// </summary>
public class EqualizerSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly BiQuadFilter[] lowFilters;
    private readonly BiQuadFilter[] highFilters;
    private readonly int channels;

    public EqualizerSampleProvider(ISampleProvider source)
    {
        this.source = source;
        channels = source.WaveFormat.Channels;

        lowFilters = new BiQuadFilter[channels];
        highFilters = new BiQuadFilter[channels];

        for (int ch = 0; ch < channels; ch++)
        {
            // 每个声道单独的滤波器，避免串扰
            lowFilters[ch] = BiQuadFilter.LowShelf(source.WaveFormat.SampleRate, 100, 0.7f, 3f);
            highFilters[ch] = BiQuadFilter.HighShelf(source.WaveFormat.SampleRate, 10000, 0.7f, 3f);
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = source.Read(buffer, offset, count);

        // 样本交错存储，需要按声道处理
        for (int i = 0; i < read; i += channels)
        {
            for (int ch = 0; ch < channels; ch++)
            {
                int idx = offset + i + ch;
                float sample = buffer[idx];

                sample = lowFilters[ch].Transform(sample);
                sample = highFilters[ch].Transform(sample);

                // 限制在 [-1,1] 避免削波
                if (sample > 1f) sample = 1f;
                else if (sample < -1f) sample = -1f;

                buffer[idx] = sample;
            }
        }

        return read;
    }

    public WaveFormat WaveFormat => source.WaveFormat;
}