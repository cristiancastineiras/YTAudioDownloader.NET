using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using YTAudioDownloader.Configuration;
using YTAudioDownloader.Models;

namespace YTAudioDownloader.Services;

public sealed class MusicBrainzService
{
    private readonly HttpClient _httpClient;
    private readonly ApiEndpointsOptions _apiEndpoints;

    public MusicBrainzService()
    {
        _apiEndpoints = AppConfiguration.Endpoints;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_apiEndpoints.MusicBrainzUserAgent);
    }

    public async Task<SongTagsData> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Search term cannot be empty.", nameof(searchTerm));
        }

        try
        {
            var cleanTerm = searchTerm.Trim();
            var query = Uri.EscapeDataString(cleanTerm);
            var searchUrl = $"{_apiEndpoints.MusicBrainzBaseUrl.TrimEnd('/')}/recording?query=recording:{query}&fmt=json&limit=5";

            var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<MbSearchResponse>(json);

            if (result?.Recordings == null || result.Recordings.Count == 0)
            {
                throw new InvalidOperationException("No recordings found in MusicBrainz.");
            }

            var recording = result.Recordings[0];
            var artist = recording.ArtistCredit?.FirstOrDefault()?.Artist?.Name ?? "Unknown";
            var albumTitle = recording.Releases?.FirstOrDefault()?.Title ?? "Unknown Album";
            var year = recording.Releases?.FirstOrDefault()?.Date?.Substring(0, 4) ?? string.Empty;
            var albumMbid = recording.Releases?.FirstOrDefault()?.Id ?? string.Empty;

            byte[]? artwork = null;
            if (!string.IsNullOrEmpty(albumMbid))
            {
                artwork = await TryDownloadCoverArtAsync(albumMbid, cancellationToken);
            }

            return new SongTagsData(
                Album: albumTitle,
                Artist: artist,
                Genre: "Unknown",
                Title: recording.Title,
                TrackNumber: 0,
                Year: year,
                AlbumArt: artwork is null
                    ? null
                    : new AlbumArtData(
                        ImageBytes: artwork,
                        Mime: "image/jpeg",
                        Description: "Cover Art",
                        Type: 3));
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"MusicBrainz API error: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse MusicBrainz response: {ex.Message}", ex);
        }
    }

    private async Task<byte[]?> TryDownloadCoverArtAsync(string albumMbid, CancellationToken cancellationToken)
    {
        try
        {
            var coverUrl = $"{_apiEndpoints.CoverArtArchiveBaseUrl.TrimEnd('/')}/release/{albumMbid}/front-500";
            var response = await _httpClient.GetAsync(coverUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    #region MusicBrainz DTOs

    private sealed class MbSearchResponse
    {
        [JsonPropertyName("recordings")]
        public List<MbRecording> Recordings { get; set; } = [];
    }

    private sealed class MbRecording
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("artist-credit")]
        public List<MbArtistCredit> ArtistCredit { get; set; } = [];

        [JsonPropertyName("releases")]
        public List<MbRelease> Releases { get; set; } = [];
    }

    private sealed class MbArtistCredit
    {
        [JsonPropertyName("artist")]
        public MbArtist Artist { get; set; } = new();
    }

    private sealed class MbArtist
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class MbRelease
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;
    }

    #endregion
}
