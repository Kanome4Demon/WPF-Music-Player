using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using TagLib;

namespace WPFMusicPlayerDemo
{
    public class MusicListManager
    {
        public ObservableCollection<MusicTrack> MusicList { get; } = new ObservableCollection<MusicTrack>();

        public void AddMusicFiles()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "音频文件|*.mp3;*.wav;*.flac;*.aac;*.wma"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                foreach (string filePath in dialog.FileNames)
                {
                    TimeSpan duration = GetAudioDuration(filePath);
                    var fileInfo = new FileInfo(filePath);
                    string format = fileInfo.Extension.TrimStart('.').ToUpper();
                    string size = (fileInfo.Length / 1024.0 / 1024.0).ToString("F2") + " MB";

                    // 使用 TagLib 读取元数据
                    string title = string.Empty;
                    string artist = string.Empty;
                    string album = string.Empty;

                    try
                    {
                        var tfile = TagLib.File.Create(filePath);
                        title = string.IsNullOrEmpty(tfile.Tag.Title) ? fileInfo.Name : tfile.Tag.Title;
                        artist = string.Join(", ", tfile.Tag.Performers);
                        album = tfile.Tag.Album ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"无法读取元数据: {filePath}, 错误: {ex.Message}");
                    }

                    var track = new MusicTrack
                    {
                        FileName = fileInfo.Name,
                        FilePath = filePath,
                        Duration = duration,
                        Title = title,
                        Artist = artist,
                        Album = album,
                        Format = format,
                        Size = size
                    };

                    MusicList.Add(track);
                }
            }
        }

        private TimeSpan GetAudioDuration(string filePath)
        {
            try
            {
                using (var reader = new AudioFileReader(filePath))
                {
                    return reader.TotalTime;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法读取音频文件时长: {filePath}, 错误: {ex.Message}");
                return TimeSpan.Zero;
            }
        }
    }
}
