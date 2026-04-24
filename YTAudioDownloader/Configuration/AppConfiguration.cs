using Microsoft.Extensions.Configuration;
using System;

namespace YTAudioDownloader.Configuration;

public static class AppConfiguration
{
    private const string EnvironmentVariablePrefix = "YTAUDIODOWNLOADER_";

    private static readonly Lazy<ApiEndpointsOptions> EndpointsLazy = new(LoadEndpoints);

    public static ApiEndpointsOptions Endpoints => EndpointsLazy.Value;

    private static ApiEndpointsOptions LoadEndpoints()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: EnvironmentVariablePrefix)
            .Build();

        return new ApiEndpointsOptions
        {
            MusicBrainzBaseUrl = GetValue(configuration, "ExternalApis:MusicBrainzBaseUrl", "https://musicbrainz.org/ws/2"),
            CoverArtArchiveBaseUrl = GetValue(configuration, "ExternalApis:CoverArtArchiveBaseUrl", "https://coverartarchive.org"),
            ItunesSearchUrl = GetValue(configuration, "ExternalApis:ItunesSearchUrl", "https://itunes.apple.com/search"),
            YouTubeImageBaseUrl = GetValue(configuration, "ExternalApis:YouTubeImageBaseUrl", "https://img.youtube.com/vi"),
            MusicBrainzUserAgent = GetValue(configuration, "ExternalApis:MusicBrainzUserAgent", "YTAudioDownloader/1.0 (Windows; +https://github.com)")
        };
    }

    private static string GetValue(IConfiguration configuration, string key, string fallback)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
