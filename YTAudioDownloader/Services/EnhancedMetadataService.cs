using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using YTAudioDownloader.Models;

namespace YTAudioDownloader.Services;

/// <summary>
/// Combina MusicBrainz + iTunes + YouTube para maximizar metadatos
/// Intenta obtener datos de ambas fuentes y combina los resultados
/// </summary>
public sealed class EnhancedMetadataService
{
    private readonly MusicBrainzService _musicBrainzService;
    private readonly SongTagsService _iTunesService;
    private readonly CoverArtService _coverArtService;

    public EnhancedMetadataService(
        MusicBrainzService? musicBrainzService = null,
        SongTagsService? iTunesService = null,
        CoverArtService? coverArtService = null)
    {
        _musicBrainzService = musicBrainzService ?? new MusicBrainzService();
        _iTunesService = iTunesService ?? new SongTagsService();
        _coverArtService = coverArtService ?? new CoverArtService();
    }

    /// <summary>
    /// Busca metadatos en MusicBrainz primero, luego en iTunes, combinando resultados
    /// </summary>
    public async Task<SongTagsData> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await SearchAsync(searchTerm, null, cancellationToken);
    }

    /// <summary>
    /// Busca metadatos + descarga portada de YouTube como fallback
    /// </summary>
    public async Task<SongTagsData> SearchAsync(
        string searchTerm,
        string? youtubeUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("El término de búsqueda no puede estar vacío.", nameof(searchTerm));
        }

        SongTagsData? mbResult = null;
        SongTagsData? itunesResult = null;

        // Intentar MusicBrainz primero (más estructurado)
        try
        {
            Debug.WriteLine($"[EnhancedMetadataService] Buscando en MusicBrainz: {searchTerm}");
            mbResult = await _musicBrainzService.SearchAsync(searchTerm, cancellationToken);
            Debug.WriteLine($"[EnhancedMetadataService] ✓ MusicBrainz encontró: {mbResult.Artist} - {mbResult.Title}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EnhancedMetadataService] ✗ MusicBrainz falló: {ex.Message}");
        }

        // Intentar iTunes como respaldo/complemento
        try
        {
            Debug.WriteLine($"[EnhancedMetadataService] Buscando en iTunes: {searchTerm}");
            itunesResult = await _iTunesService.SearchAsync(searchTerm, cancellationToken);
            Debug.WriteLine($"[EnhancedMetadataService] ✓ iTunes encontró: {itunesResult.Artist} - {itunesResult.Title}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EnhancedMetadataService] ✗ iTunes falló: {ex.Message}");
        }

        // Si ninguno funcionó, lanzar error
        if (mbResult is null && itunesResult is null)
        {
            throw new InvalidOperationException("No se pudieron obtener metadatos de MusicBrainz ni iTunes.");
        }

        // Combinar resultados: MusicBrainz como primario, iTunes como respaldo
        var result = CombineResults(mbResult, itunesResult);

        // Si no hay portada, intentar descargar del thumbnail de YouTube
        if (result.AlbumArt is null && !string.IsNullOrWhiteSpace(youtubeUrl))
        {
            try
            {
                Debug.WriteLine($"[EnhancedMetadataService] Intentando descargar portada de YouTube");
                var videoId = CoverArtService.ExtractVideoIdFromUrl(youtubeUrl);
                if (!string.IsNullOrWhiteSpace(videoId))
                {
                    var youtubeCover = await _coverArtService.TryDownloadYouTubeCoverAsync(videoId, cancellationToken);
                    if (youtubeCover is not null)
                    {
                        Debug.WriteLine($"[EnhancedMetadataService] ✓ Portada descargada de YouTube");
                        result = result with
                        {
                            AlbumArt = new AlbumArtData(
                                ImageBytes: youtubeCover,
                                Mime: "image/jpeg",
                                Description: "YouTube Thumbnail",
                                Type: 3)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EnhancedMetadataService] ✗ No se pudo descargar portada de YouTube: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Combina resultados de ambas fuentes, priorizando MusicBrainz pero rellenando huecos con iTunes
    /// </summary>
    private static SongTagsData CombineResults(SongTagsData? mbResult, SongTagsData? itunesResult)
    {
        if (mbResult is not null && itunesResult is null)
        {
            // Solo MusicBrainz disponible
            return mbResult;
        }

        if (mbResult is null && itunesResult is not null)
        {
            // Solo iTunes disponible
            return itunesResult;
        }

        // Combinar ambos: MusicBrainz primario, iTunes complementario
        var combined = new SongTagsData(
            Album: GetBestValue(mbResult!.Album, itunesResult!.Album),
            Artist: GetBestValue(mbResult.Artist, itunesResult.Artist),
            Genre: GetBestValue(mbResult.Genre, itunesResult.Genre, preferNonUnknown: true),
            Title: GetBestValue(mbResult.Title, itunesResult.Title),
            TrackNumber: GetBestTrackNumber(mbResult.TrackNumber, itunesResult.TrackNumber),
            Year: GetBestValue(mbResult.Year, itunesResult.Year, preferNonEmpty: true),
            AlbumArt: GetBestAlbumArt(mbResult.AlbumArt, itunesResult.AlbumArt));

        return combined;
    }

    /// <summary>
    /// Selecciona el mejor valor entre dos opciones
    /// </summary>
    private static string GetBestValue(
        string? value1,
        string? value2,
        bool preferNonEmpty = false,
        bool preferNonUnknown = false)
    {
        var isEmpty1 = string.IsNullOrWhiteSpace(value1);
        var isEmpty2 = string.IsNullOrWhiteSpace(value2);

        // Si ambos están vacíos
        if (isEmpty1 && isEmpty2)
        {
            return preferNonUnknown ? "Unknown" : string.Empty;
        }

        // Si uno está vacío, retornar el otro
        if (isEmpty1)
        {
            return value2 ?? string.Empty;
        }

        if (isEmpty2)
        {
            return value1 ?? string.Empty;
        }

        // Ambos tienen valor, preferir valor1 (MusicBrainz)
        if (preferNonUnknown && value1?.Equals("Unknown", StringComparison.OrdinalIgnoreCase) == true)
        {
            return value2!;
        }

        return value1!;
    }

    /// <summary>
    /// Selecciona el mejor número de pista
    /// </summary>
    private static int GetBestTrackNumber(int track1, int track2)
    {
        // Si track1 es válido (>0), usarlo; si no, usar track2
        return track1 > 0 ? track1 : track2;
    }

    /// <summary>
    /// Selecciona la mejor portada (prioriza la que exista)
    /// </summary>
    private static AlbumArtData? GetBestAlbumArt(AlbumArtData? art1, AlbumArtData? art2)
    {
        if (art1 is not null)
        {
            return art1; // MusicBrainz
        }

        return art2; // iTunes como respaldo
    }
}
