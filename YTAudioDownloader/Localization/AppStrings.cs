namespace YTAudioDownloader.Localization;

public enum AppLanguage
{
    Spanish,
    English
}

public static class AppStrings
{
    private static AppLanguage _currentLanguage = AppLanguage.Spanish;

    public static AppLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set => _currentLanguage = value;
    }

    public static class Common
    {
        public static string Download => _currentLanguage == AppLanguage.Spanish ? "Descargar" : "Download";
        public static string Cancel => _currentLanguage == AppLanguage.Spanish ? "Cancelar" : "Cancel";
        public static string Browse => _currentLanguage == AppLanguage.Spanish ? "Examinar" : "Browse";
        public static string Error => _currentLanguage == AppLanguage.Spanish ? "Error" : "Error";
        public static string Ready => _currentLanguage == AppLanguage.Spanish ? "Listo para descargar." : "Ready to download.";
        public static string Activity => _currentLanguage == AppLanguage.Spanish ? "Actividad" : "Activity";
    }

    public static class MainWindow
    {
        public static string Title => _currentLanguage == AppLanguage.Spanish ? "YT Audio Downloader" : "YT Audio Downloader";
        public static string Subtitle => _currentLanguage == AppLanguage.Spanish ? "<3" : "<3";
        public static string YouTubeUrl => _currentLanguage == AppLanguage.Spanish ? "Video de YouTube" : "YouTube Video";
        public static string YouTubePlaceholder => _currentLanguage == AppLanguage.Spanish ? "https://www.youtube.com/watch?v=..." : "https://www.youtube.com/watch?v=...";
        public static string OutputFolder => _currentLanguage == AppLanguage.Spanish ? "Carpeta de salida" : "Output Folder";
        public static string OutputPlaceholder => _currentLanguage == AppLanguage.Spanish ? "Selecciona una carpeta" : "Select a folder";
        public static string Format => _currentLanguage == AppLanguage.Spanish ? "Formato de audio" : "Audio Format";
        public static string Quality => _currentLanguage == AppLanguage.Spanish ? "Calidad" : "Quality";
        public static string FormatNote => _currentLanguage == AppLanguage.Spanish
            ? "Nota: FLAC/WAV no tienen opción de calidad."
            : "Note: FLAC/WAV have no quality option.";
        public static string Downloading => _currentLanguage == AppLanguage.Spanish ? "Descargando audio..." : "Downloading audio...";
        public static string Converting => _currentLanguage == AppLanguage.Spanish ? "Convirtiendo a {0}..." : "Converting to {0}...";
        public static string SearchingMetadata => _currentLanguage == AppLanguage.Spanish ? "Buscando metadatos en MusicBrainz + iTunes..." : "Searching metadata in MusicBrainz + iTunes...";
        public static string ApplyingTags => _currentLanguage == AppLanguage.Spanish ? "Aplicando tags y portada..." : "Applying tags and cover...";
        public static string ProcessCompleted => _currentLanguage == AppLanguage.Spanish ? "Proceso completado sin errores." : "Process completed successfully.";
        public static string Cancelled => _currentLanguage == AppLanguage.Spanish ? "Proceso cancelado por el usuario." : "Process cancelled by user.";
        public static string MetadataApplied => _currentLanguage == AppLanguage.Spanish ? "Metadatos aplicados correctamente." : "Metadata applied successfully.";
        public static string MetadataWarning => _currentLanguage == AppLanguage.Spanish
            ? "Advertencia: No se pudieron obtener metadatos. El audio se guardó sin ellos."
            : "Warning: Could not obtain metadata. Audio was saved without it.";
        public static string InvalidUrl => _currentLanguage == AppLanguage.Spanish ? "URL inválida. Revisa el enlace de YouTube." : "Invalid URL. Check the YouTube link.";
        public static string FolderNotFound => _currentLanguage == AppLanguage.Spanish ? "La carpeta de salida no existe." : "Output folder does not exist.";
    }

    public static class LanguageMenu
    {
        public static string SelectLanguage => _currentLanguage == AppLanguage.Spanish ? "Idioma" : "Language";
        public static string Spanish => "Español";
        public static string English => "English";
    }
}
