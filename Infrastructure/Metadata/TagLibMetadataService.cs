using System;
using System.IO;
using TagLib;

namespace WPFMusicPlayerDemo.Services
{
    public class TagLibMetadataService : IMetadataService
    {
        public (string Title, string Artist, string Album) GetMetadata(string filePath)
        {
            try
            {
                // 直接使用 TagLib.File.Create 解析元数据
                var tagFile = TagLib.File.Create(filePath);

                string title = string.IsNullOrEmpty(tagFile.Tag.Title) ? Path.GetFileName(filePath) : tagFile.Tag.Title;
                string artist = string.Join(", ", tagFile.Tag.Performers);
                string album = tagFile.Tag.Album ?? string.Empty;

                return (title, artist, album);
            }
            catch
            {
                // 如果解析失败，返回文件名作为标题，其它为空
                return (Path.GetFileName(filePath), string.Empty, string.Empty);
            }
        }
    }
}
