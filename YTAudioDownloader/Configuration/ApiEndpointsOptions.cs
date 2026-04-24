namespace YTAudioDownloader.Configuration;

public sealed class ApiEndpointsOptions
{
    public string MusicBrainzBaseUrl { get; set; } = "https://musicbrainz.org/ws/2";
    public string CoverArtArchiveBaseUrl { get; set; } = "https://coverartarchive.org";
    public string ItunesSearchUrl { get; set; } = "https://itunes.apple.com/search";
    public string YouTubeImageBaseUrl { get; set; } = "https://img.youtube.com/vi";
    public string MusicBrainzUserAgent { get; set; } = "YTAudioDownloader/1.0 (Windows; +https://github.com)";
}
