using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YTAudioDownloader.Localization;
using YTAudioDownloader.Models;
using YTAudioDownloader.Services;
using Windows.Storage.Pickers;

namespace YTAudioDownloader
{
    public sealed partial class MainWindow : Window
    {
        private readonly YouTubeAudioService _youTubeAudioService = new();
        private readonly AudioConversionService _audioConversionService = new();
        private readonly EnhancedMetadataService _enhancedMetadataService = new();
        private readonly SongTagsService _songTagsService = new();
        private readonly ObservableCollection<string> _logs = [];
        private CancellationTokenSource? _downloadCts;

        public MainWindow()
        {
            InitializeComponent();

            var defaultMusicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            var folderPath = Directory.Exists(defaultMusicFolder)
                ? defaultMusicFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            
            if (OutputFolderTextBox != null)
            {
                OutputFolderTextBox.Text = folderPath;
            }

            if (LogListView != null)
            {
                LogListView.ItemsSource = _logs;
            }
            
            // Actualizar strings DESPUÉS de que todo esté inicializado
            UpdateStrings();
            AddLog(AppStrings.CurrentLanguage == AppLanguage.Spanish ? "Aplicación iniciada." : "Application started.");
        }

        private void UpdateStrings()
        {
            // Verificar que los controles no sean null antes de accederlos
            if (TitleBlock != null) TitleBlock.Text = AppStrings.MainWindow.Title;
            if (SubtitleBlock != null) SubtitleBlock.Text = AppStrings.MainWindow.Subtitle;
            if (YouTubeUrlLabel != null) YouTubeUrlLabel.Text = AppStrings.MainWindow.YouTubeUrl;
            if (VideoUrlTextBox != null) VideoUrlTextBox.PlaceholderText = AppStrings.MainWindow.YouTubePlaceholder;
            if (OutputFolderLabel != null) OutputFolderLabel.Text = AppStrings.MainWindow.OutputFolder;
            if (OutputFolderTextBox != null) OutputFolderTextBox.PlaceholderText = AppStrings.MainWindow.OutputPlaceholder;
            if (FormatLabel != null) FormatLabel.Text = AppStrings.MainWindow.Format;
            if (FormatNoteBlock != null) FormatNoteBlock.Text = AppStrings.MainWindow.FormatNote;
            if (ActivityLabel != null) ActivityLabel.Text = AppStrings.Common.Activity;
            if (BrowseButton != null) BrowseButton.Content = AppStrings.Common.Browse;
            if (DownloadButton != null) DownloadButton.Content = AppStrings.Common.Download;
            if (CancelButton != null) CancelButton.Content = AppStrings.Common.Cancel;
            if (StatusTextBlock != null) StatusTextBlock.Text = AppStrings.Common.Ready;
            if (FooterBlock != null)
            {
                FooterBlock.Text = AppStrings.CurrentLanguage == AppLanguage.Spanish
                    ? "Se aplican metadatos automáticos usando MusicBrainz + iTunes (+ YouTube como portada alternativa)."
                    : "Automatic metadata using MusicBrainz + iTunes (+ YouTube as fallback cover).";
            }
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            AppStrings.CurrentLanguage = LanguageComboBox.SelectedIndex == 0 ? AppLanguage.Spanish : AppLanguage.English;
            UpdateStrings();
            AddLog(AppStrings.CurrentLanguage == AppLanguage.Spanish ? "Idioma cambiado a español." : "Language changed to English.");
        }

        private async void OnBrowseFolderClick(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, windowHandle);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }

            if (OutputFolderTextBox != null)
            {
                OutputFolderTextBox.Text = folder.Path;
            }
            AddLog($"Carpeta seleccionada: {folder.Path}");
        }

        private async void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            var videoUrl = VideoUrlTextBox?.Text?.Trim() ?? string.Empty;
            var outputFolder = OutputFolderTextBox?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                SetStatus(AppStrings.MainWindow.InvalidUrl, true);
                return;
            }

            if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out _))
            {
                SetStatus(AppStrings.MainWindow.InvalidUrl, true);
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                SetStatus(AppStrings.MainWindow.FolderNotFound, true);
                return;
            }

            _downloadCts?.Dispose();
            _downloadCts = new CancellationTokenSource();

            SetRunningState(true);
            if (DownloadProgressBar != null) DownloadProgressBar.Value = 0;
            SetStatus(string.Format(AppStrings.MainWindow.Converting, "Audio"), false);
            AddLog(AppStrings.CurrentLanguage == AppLanguage.Spanish ? "Iniciando descarga de audio." : "Starting audio download.");

            var progress = new Progress<double>(value =>
            {
                if (DownloadProgressBar != null) DownloadProgressBar.Value = value;
                if (StatusTextBlock != null) StatusTextBlock.Text = string.Format(AppStrings.MainWindow.Downloading + " {0:0}%", value);
            });

            try
            {
                var downloadedAudio = await _youTubeAudioService.DownloadAudioAsync(
                    videoUrl,
                    outputFolder,
                    progress,
                    _downloadCts.Token);

                AddLog($"Audio fuente descargado: {downloadedAudio.FilePath}");

                // Convertir al formato y calidad seleccionados
                var selectedFormat = (AudioFormat)(FormatComboBox?.SelectedIndex ?? 0);
                var selectedQuality = GetQualityFromIndex(QualityComboBox?.SelectedIndex ?? 1);
                var conversionSettings = new AudioConversionSettings(selectedFormat, selectedQuality);

                SetStatus(string.Format(AppStrings.MainWindow.Converting, conversionSettings.FormatName), false);
                var conversionMsg = AppStrings.CurrentLanguage == AppLanguage.Spanish
                    ? $"Convirtiendo a {conversionSettings.FormatName} ({conversionSettings.QualityDescription}). Primera vez puede tardar unos segundos."
                    : $"Converting to {conversionSettings.FormatName} ({conversionSettings.QualityDescription}). First time may take a few seconds.";
                AddLog(conversionMsg);

                var conversionProgress = new Progress<double>(value =>
                {
                    if (DownloadProgressBar != null) DownloadProgressBar.Value = value;
                    if (StatusTextBlock != null) StatusTextBlock.Text = string.Format(AppStrings.MainWindow.Converting + " {0:0}%", conversionSettings.FormatName, value);
                });

                var outputPath = await _audioConversionService.ConvertAsync(
                    downloadedAudio.FilePath,
                    outputFolder,
                    downloadedAudio.VideoTitle,
                    conversionSettings,
                    conversionProgress,
                    _downloadCts.Token);

                var finalAudio = downloadedAudio with
                {
                    FilePath = outputPath,
                    ContainerExtension = conversionSettings.FileExtension
                };

                TryDeleteSourceFile(downloadedAudio.FilePath, outputPath);
                var completeMsg = AppStrings.CurrentLanguage == AppLanguage.Spanish
                    ? $"Conversión a {conversionSettings.FormatName} completada: {outputPath}"
                    : $"Conversion to {conversionSettings.FormatName} completed: {outputPath}";
                AddLog(completeMsg);

                // Aplicar tags automáticamente con URL de YouTube para portada alternativa
                await ApplyTagsAsync(finalAudio, videoUrl, _downloadCts.Token);

                if (DownloadProgressBar != null) DownloadProgressBar.Value = 100;
                SetStatus(AppStrings.MainWindow.ProcessCompleted, false);
                var endMsg = AppStrings.CurrentLanguage == AppLanguage.Spanish ? "Proceso completado sin errores." : "Process completed successfully.";
                AddLog(endMsg);
            }
            catch (OperationCanceledException)
            {
                SetStatus(AppStrings.MainWindow.Cancelled, true);
                var cancelMsg = AppStrings.CurrentLanguage == AppLanguage.Spanish ? "Descarga cancelada." : "Download cancelled.";
                AddLog(cancelMsg);
            }
            catch (Exception ex)
            {
                SetStatus($"{AppStrings.Common.Error}: {ex.Message}", true);
                AddLog($"ERROR: {ex}");
            }
            finally
            {
                SetRunningState(false);
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            _downloadCts?.Cancel();
        }

        private async Task ApplyTagsAsync(
            DownloadAudioResult downloadResult,
            string? youtubeUrl,
            CancellationToken cancellationToken)
        {
            try
            {
                var searchMsg = AppStrings.CurrentLanguage == AppLanguage.Spanish
                    ? $"Buscando metadatos para: {downloadResult.VideoTitle}"
                    : $"Searching metadata for: {downloadResult.VideoTitle}";
                AddLog(searchMsg);
                SetStatus(AppStrings.MainWindow.SearchingMetadata, false);

                var tags = await _enhancedMetadataService.SearchAsync(
                    downloadResult.VideoTitle,
                    youtubeUrl,
                    cancellationToken);
                await _songTagsService.ApplyTagsAsync(downloadResult.FilePath, tags, cancellationToken);

                var tagsMsg = AppStrings.CurrentLanguage == AppLanguage.Spanish
                    ? $"Metadatos aplicados: {tags.Artist} - {tags.Title} ({tags.Album})"
                    : $"Metadata applied: {tags.Artist} - {tags.Title} ({tags.Album})";
                AddLog(tagsMsg);
                var coverMsg = AppStrings.CurrentLanguage == AppLanguage.Spanish ? "Portada agregada al archivo." : "Cover added to file.";
                AddLog(coverMsg);
                SetStatus(AppStrings.MainWindow.MetadataApplied, false);
            }
            catch (Exception ex)
            {
                var warnMsg = AppStrings.CurrentLanguage == AppLanguage.Spanish
                    ? $"Advertencia: No se pudieron obtener metadatos ({ex.Message}). El audio se guardó sin ellos."
                    : $"Warning: Could not get metadata ({ex.Message}). Audio saved without it.";
                AddLog(warnMsg);
                SetStatus(AppStrings.MainWindow.MetadataWarning, false);
            }
        }

        private void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logs.Add($"[{timestamp}] {message}");

            // Scroll al último item solo si LogListView está inicializado
            if (LogListView != null && _logs.Count > 0)
            {
                LogListView.ScrollIntoView(_logs[^1]);
            }
        }

        private void SetStatus(string message, bool isError)
        {
            if (StatusTextBlock == null) return;
            
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(
                    255,
                    isError ? (byte)207 : (byte)9,
                    isError ? (byte)34 : (byte)105,
                    isError ? (byte)46 : (byte)218));
        }

        private static AudioQuality GetQualityFromIndex(int index)
        {
            return index switch
            {
                0 => AudioQuality.Low,
                1 => AudioQuality.Medium,
                2 => AudioQuality.High,
                3 => AudioQuality.VeryHigh,
                _ => AudioQuality.Medium
            };
        }

        private void TryDeleteSourceFile(string sourceFilePath, string finalFilePath)
        {
            try
            {
                if (!sourceFilePath.Equals(finalFilePath, StringComparison.OrdinalIgnoreCase)
                    && File.Exists(sourceFilePath))
                {
                    File.Delete(sourceFilePath);
                }
            }
            catch
            {
                AddLog("No se pudo eliminar el archivo temporal original (se conserva en disco).");
            }
        }

        private void SetRunningState(bool isRunning)
        {
            if (DownloadButton != null) DownloadButton.IsEnabled = !isRunning;
            if (CancelButton != null) CancelButton.IsEnabled = isRunning;
            if (VideoUrlTextBox != null) VideoUrlTextBox.IsEnabled = !isRunning;
            if (OutputFolderTextBox != null) OutputFolderTextBox.IsEnabled = !isRunning;
        }
    }
}
