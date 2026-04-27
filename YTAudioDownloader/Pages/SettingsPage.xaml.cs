using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using YTAudioDownloader.Models;
using YTAudioDownloader.State;

namespace YTAudioDownloader.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _isLoading;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        LoadSettings();
    }

    private void LoadSettings()
    {
        _isLoading = true;
        var state = AppState.Instance;

        OutputFolderBox.Text = state.OutputFolder;
        ArtistSubfolderSwitch.IsOn = state.CreateArtistSubfolders;
        OpenFolderSwitch.IsOn = state.OpenFolderOnComplete;
        GpuAccelSwitch.IsOn = state.UseGpuAcceleration;
        LowPowerSwitch.IsOn = state.LowPowerMode;
        AutoMetadataSwitch.IsOn = state.AutoMetadata;

        DefaultFormatCombo.SelectedIndex = state.DefaultFormat switch
        {
            AudioFormat.Mp3 => 0,
            AudioFormat.Flac => 1,
            AudioFormat.Wav => 2,
            AudioFormat.Aac => 3,
            AudioFormat.Opus => 4,
            _ => 0
        };

        DefaultQualityCombo.SelectedIndex = state.DefaultQuality switch
        {
            AudioQuality.Low => 0,
            AudioQuality.Medium => 1,
            AudioQuality.High => 2,
            AudioQuality.VeryHigh => 3,
            _ => 3
        };

        SimultaneousCombo.SelectedIndex = Math.Clamp(state.MaxSimultaneousDownloads - 1, 0, 4);

        _isLoading = false;
    }

    private async void OnBrowseFolderClick(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        folderPicker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
            App.MainAppWindow ?? throw new InvalidOperationException("No window"));
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder is null) return;

        AppState.Instance.OutputFolder = folder.Path;
        OutputFolderBox.Text = folder.Path;
    }

    private void OnArtistSubfolderToggled(object sender, RoutedEventArgs e)
    {
        if (!_isLoading) AppState.Instance.CreateArtistSubfolders = ArtistSubfolderSwitch.IsOn;
    }

    private void OnOpenFolderToggled(object sender, RoutedEventArgs e)
    {
        if (!_isLoading) AppState.Instance.OpenFolderOnComplete = OpenFolderSwitch.IsOn;
    }

    private void OnGpuAccelToggled(object sender, RoutedEventArgs e)
    {
        if (!_isLoading) AppState.Instance.UseGpuAcceleration = GpuAccelSwitch.IsOn;
    }

    private void OnLowPowerToggled(object sender, RoutedEventArgs e)
    {
        if (!_isLoading) AppState.Instance.LowPowerMode = LowPowerSwitch.IsOn;
    }

    private void OnAutoMetadataToggled(object sender, RoutedEventArgs e)
    {
        if (!_isLoading) AppState.Instance.AutoMetadata = AutoMetadataSwitch.IsOn;
    }

    private void OnDefaultFormatChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        AppState.Instance.DefaultFormat = DefaultFormatCombo.SelectedIndex switch
        {
            1 => AudioFormat.Flac,
            2 => AudioFormat.Wav,
            3 => AudioFormat.Aac,
            4 => AudioFormat.Opus,
            _ => AudioFormat.Mp3
        };
    }

    private void OnDefaultQualityChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        AppState.Instance.DefaultQuality = DefaultQualityCombo.SelectedIndex switch
        {
            0 => AudioQuality.Low,
            1 => AudioQuality.Medium,
            2 => AudioQuality.High,
            _ => AudioQuality.VeryHigh
        };
    }

    private void OnSimultaneousChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        AppState.Instance.MaxSimultaneousDownloads = (SimultaneousCombo.SelectedIndex + 1);
    }

    private void OnGitHubClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com") { UseShellExecute = true });
    }
}
