using System;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YTAudioDownloader.Configuration;
using YTAudioDownloader.Models;

namespace YTAudioDownloader.Services;

public sealed class SongTagsService
{
    private static readonly Regex ParenthesizedTextRegex = new(@"\s*\([^)]*\)", RegexOptions.Compiled);
    private readonly HttpClient _httpClient;
    private readonly ApiEndpointsOptions _apiEndpoints;

    public SongTagsService(HttpClient? httpClient = null)
    {
        _apiEndpoints = AppConfiguration.Endpoints;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<SongTagsData> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Debes indicar un termino para buscar tags.", nameof(searchTerm));
        }

        var cleanTerm = RemoveParenthesizedText(searchTerm).Trim();
        if (string.IsNullOrWhiteSpace(cleanTerm))
        {
            throw new InvalidOperationException("El termino de busqueda quedo vacio tras limpiar parentesis.");
        }

        var uriBuilder = new UriBuilder(_apiEndpoints.ItunesSearchUrl);
        var query = $"media=music&term={WebUtility.UrlEncode(cleanTerm)}";
        uriBuilder.Query = query;

        using var response = await _httpClient.GetAsync(uriBuilder.Uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<ItunesSearchResponse>(payload)
            ?? throw new InvalidOperationException("No se pudo parsear la respuesta de iTunes.");

        if (data.ResultCount <= 0 || data.Results.Count == 0)
        {
            throw new InvalidOperationException("La API de iTunes no devolvio resultados.");
        }

        var result = data.Results[0];
        var artworkUrl = result.ArtworkUrl100.Replace("100x100bb.jpg", "600x600bb.jpg", StringComparison.OrdinalIgnoreCase);
        byte[]? artwork = null;

        if (Uri.TryCreate(artworkUrl, UriKind.Absolute, out var artworkUri))
        {
            artwork = await _httpClient.GetByteArrayAsync(artworkUri, cancellationToken);
        }

        return new SongTagsData(
            Album: result.CollectionName,
            Artist: result.ArtistName,
            Genre: result.PrimaryGenreName,
            Title: result.TrackName,
            TrackNumber: result.TrackNumber,
            Year: TryExtractYear(result.ReleaseDate),
            AlbumArt: artwork is null
                ? null
                : new AlbumArtData(
                    ImageBytes: artwork,
                    Mime: "image/jpeg",
                    Description: "Album Art",
                    Type: 3));
    }

    public Task ApplyTagsAsync(string filePath, SongTagsData tags, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new FileNotFoundException("No se encontro el archivo de audio para etiquetar.", filePath);
        }

        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;

        tag.Album = tags.Album;
        tag.Title = tags.Title;
        tag.Performers = [tags.Artist];
        tag.Genres = [tags.Genre];

        if (uint.TryParse(tags.Year, out var parsedYear))
        {
            tag.Year = parsedYear;
        }

        if (tags.TrackNumber > 0)
        {
            tag.Track = (uint)tags.TrackNumber;
        }

        if (tags.AlbumArt is not null)
        {
            var picture = new TagLib.Picture(new TagLib.ByteVector(tags.AlbumArt.ImageBytes))
            {
                MimeType = tags.AlbumArt.Mime,
                Description = tags.AlbumArt.Description,
                Type = (TagLib.PictureType)tags.AlbumArt.Type
            };

            tag.Pictures = [picture];
        }

        file.Save();
        return Task.CompletedTask;
    }

    private static string RemoveParenthesizedText(string value) => ParenthesizedTextRegex.Replace(value, string.Empty);

    private static string TryExtractYear(string releaseDate)
    {
        if (!string.IsNullOrWhiteSpace(releaseDate) && releaseDate.Length >= 4)
        {
            return releaseDate[..4];
        }

        return string.Empty;
    }
}
