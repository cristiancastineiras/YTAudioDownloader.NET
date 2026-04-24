using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace YTAudioDownloader.Models;

public sealed class ItunesSearchResponse
{
    [JsonPropertyName("resultCount")]
    public int ResultCount { get; init; }

    [JsonPropertyName("results")]
    public List<ItunesTrackResult> Results { get; init; } = [];
}

public sealed class ItunesTrackResult
{
    [JsonPropertyName("artistName")]
    public string ArtistName { get; init; } = string.Empty;

    [JsonPropertyName("artworkUrl100")]
    public string ArtworkUrl100 { get; init; } = string.Empty;

    [JsonPropertyName("collectionName")]
    public string CollectionName { get; init; } = string.Empty;

    [JsonPropertyName("primaryGenreName")]
    public string PrimaryGenreName { get; init; } = string.Empty;

    [JsonPropertyName("releaseDate")]
    public string ReleaseDate { get; init; } = string.Empty;

    [JsonPropertyName("trackName")]
    public string TrackName { get; init; } = string.Empty;

    [JsonPropertyName("trackNumber")]
    public int TrackNumber { get; init; }
}
