using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using YTAudioDownloader.Pages;

namespace YTAudioDownloader;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Extend content into title bar for seamless look
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Set a reasonable minimum window size
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1100, 720));

        // Navigate to the download page on startup
        ContentFrame.Navigate(typeof(DownloadPage));
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || ContentFrame is null) return;

        var tag = rb.Tag?.ToString();
        var pageType = tag switch
        {
            "download" => typeof(DownloadPage),
            "queue"    => typeof(QueuePage),
            "library"  => typeof(LibraryPage),
            "settings" => typeof(SettingsPage),
            _          => typeof(DownloadPage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    // Called from Pages when they need to navigate programmatically
    public void NavigateToQueue()
    {
        NavQueue.IsChecked = true;
    }
}
