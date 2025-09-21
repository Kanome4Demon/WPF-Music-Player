namespace WPFMusicPlayerDemo.Services
{
    public interface IMetadataService
    {
        (string Title, string Artist, string Album) GetMetadata(string filePath);
    }
}
