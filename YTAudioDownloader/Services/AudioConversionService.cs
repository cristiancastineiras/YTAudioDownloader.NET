using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using YTAudioDownloader.Models;

namespace YTAudioDownloader.Services;

public sealed class AudioConversionService
{
    private static readonly SemaphoreSlim InitializationGate = new(1, 1);
    private static bool _isInitialized;

    public async Task<string> ConvertAsync(
        string sourceFilePath,
        string targetDirectory,
        string outputFileName,
        AudioConversionSettings settings,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException($"No existe el archivo de entrada para convertir a {settings.FormatName}.", sourceFilePath);
        }

        if (string.IsNullOrWhiteSpace(targetDirectory) || !Directory.Exists(targetDirectory))
        {
            throw new DirectoryNotFoundException($"La carpeta de destino para {settings.FormatName} no existe.");
        }

        await EnsureFfmpegReadyAsync(cancellationToken);

        var cleanName = SanitizeFileName(outputFileName);
        var targetPath = BuildUniqueOutputPath(targetDirectory, cleanName, settings.FileExtension);

        var conversion = await FFmpeg.Conversions.FromSnippet.Convert(sourceFilePath, targetPath);
        conversion.SetOverwriteOutput(true);

        var audioParams = BuildAudioParameters(settings);
        conversion.AddParameter($"-vn {audioParams}", ParameterPosition.PostInput);

        if (progress is not null)
        {
            conversion.OnProgress += (_, args) =>
            {
                progress.Report(Math.Clamp(args.Percent, 0d, 100d));
            };
        }

        await conversion.Start(cancellationToken);
        return targetPath;
    }

    public async Task<string> ConvertToMp3Async(
        string sourceFilePath,
        string targetDirectory,
        string outputFileName,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var settings = new AudioConversionSettings(AudioFormat.Mp3, AudioQuality.High);
        return await ConvertAsync(sourceFilePath, targetDirectory, outputFileName, settings, progress, cancellationToken);
    }

    private static async Task EnsureFfmpegReadyAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized)
        {
            return;
        }

        await InitializationGate.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            var ffmpegPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YTAudioDownloader",
                "ffmpeg");

            Directory.CreateDirectory(ffmpegPath);
            FFmpeg.SetExecutablesPath(ffmpegPath);

            var ffmpegExe = Path.Combine(ffmpegPath, "ffmpeg.exe");
            if (!File.Exists(ffmpegExe))
            {
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
                FFmpeg.SetExecutablesPath(ffmpegPath);
            }

            _isInitialized = true;
        }
        finally
        {
            InitializationGate.Release();
        }
    }

    private static string BuildUniqueOutputPath(string folder, string fileNameWithoutExtension, string extension)
    {
        var basePath = Path.Combine(folder, $"{fileNameWithoutExtension}.{extension}");
        if (!File.Exists(basePath))
        {
            return basePath;
        }

        var index = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(folder, $"{fileNameWithoutExtension} ({index}).{extension}");
            index++;
        }
        while (File.Exists(candidate));

        return candidate;
    }

    private static string BuildAudioParameters(AudioConversionSettings settings)
    {
        return settings.Format switch
        {
            AudioFormat.Mp3 => $"-codec:a libmp3lame -b:a {(int)settings.Quality}k",
            AudioFormat.Aac => $"-codec:a aac -b:a {(int)settings.Quality}k",
            AudioFormat.Opus => $"-codec:a libopus -b:a {(int)settings.Quality}k",
            AudioFormat.Flac => "-codec:a flac -q:a 8",
            AudioFormat.Wav => "-codec:a pcm_s16le",
            _ => "-codec:a libmp3lame -b:a 192k"
        };
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = value.Trim();

        foreach (var invalid in invalidChars)
        {
            sanitized = sanitized.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "audio" : sanitized;
    }
}
