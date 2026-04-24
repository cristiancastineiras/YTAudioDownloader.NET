namespace YTAudioDownloader.Models;

public sealed record AlbumArtData(
    byte[] ImageBytes,
    string Mime,
    string Description,
    int Type
);

public sealed record SongTagsData(
    string Album,
    string Artist,
    string Genre,
    string Title,
    int TrackNumber,
    string Year,
    AlbumArtData? AlbumArt
);
