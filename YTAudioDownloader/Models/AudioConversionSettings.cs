namespace YTAudioDownloader.Models;

public enum AudioFormat
{
    Mp3,
    Aac,
    Opus,
    Flac,
    Wav
}

public enum AudioQuality
{
    Low = 128,
    Medium = 192,
    High = 256,
    VeryHigh = 320
}

public sealed record AudioConversionSettings(
    AudioFormat Format,
    AudioQuality Quality
)
{
    public string FormatName => Format switch
    {
        AudioFormat.Mp3 => "MP3",
        AudioFormat.Aac => "AAC",
        AudioFormat.Opus => "Opus",
        AudioFormat.Flac => "FLAC",
        AudioFormat.Wav => "WAV",
        _ => "Desconocido"
    };

    public string QualityDescription => Format switch
    {
        AudioFormat.Flac or AudioFormat.Wav => "Lossless (sin pérdida)",
        _ => $"{(int)Quality} kbps"
    };

    public string FileExtension => Format switch
    {
        AudioFormat.Mp3 => "mp3",
        AudioFormat.Aac => "m4a",
        AudioFormat.Opus => "opus",
        AudioFormat.Flac => "flac",
        AudioFormat.Wav => "wav",
        _ => "audio"
    };
}
