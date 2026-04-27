using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using YTAudioDownloader.Models;
using YTAudioDownloader.State;

namespace YTAudioDownloader.Pages;

public sealed partial class LibraryPage : Page
{
    private readonly ObservableCollection<DownloadQueueItem> _displayItems = new();
    private string _searchQuery = string.Empty;
    private bool _isGridView = true;

    public LibraryPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        AppState.Instance.Downloads.CollectionChanged += OnDownloadsChanged;
        foreach (var item in AppState.Instance.Downloads)
            item.PropertyChanged += OnItemPropertyChanged;

        RefreshDisplay();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        AppState.Instance.Downloads.CollectionChanged -= OnDownloadsChanged;
        foreach (var item in AppState.Instance.Downloads)
            item.PropertyChanged -= OnItemPropertyChanged;
    }

    private void OnDownloadsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (DownloadQueueItem item in e.NewItems)
                item.PropertyChanged += OnItemPropertyChanged;

        if (e.OldItems != null)
            foreach (DownloadQueueItem item in e.OldItems)
                item.PropertyChanged -= OnItemPropertyChanged;

        DispatcherQueue.TryEnqueue(RefreshDisplay);
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DownloadQueueItem.Status))
            DispatcherQueue.TryEnqueue(RefreshDisplay);
    }

    private void RefreshDisplay()
    {
        var completed = AppState.Instance.Downloads
            .Where(d => d.Status == DownloadStatus.Completed)
            .Where(d => string.IsNullOrEmpty(_searchQuery)
                || d.Title.Contains(_searchQuery, System.StringComparison.OrdinalIgnoreCase)
                || d.Artist.Contains(_searchQuery, System.StringComparison.OrdinalIgnoreCase))
            .ToList();

        GridItemsControl.ItemsSource = completed;
        ListItemsControl.ItemsSource = completed;

        var total = AppState.Instance.Downloads.Count(d => d.Status == DownloadStatus.Completed);
        CountLabel.Text = $"{total} canción{(total != 1 ? "es" : "")} descargada{(total != 1 ? "s" : "")}";

        EmptyState.Visibility = completed.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchQuery = SearchBox.Text;
        RefreshDisplay();
    }

    private void OnGridViewClick(object sender, RoutedEventArgs e)
    {
        _isGridView = true;
        GridItemsControl.Visibility = Visibility.Visible;
        ListViewContainer.Visibility = Visibility.Collapsed;
        GridViewBtn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(255, 255, 255, 255));
        ListViewBtn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    private void OnListViewClick(object sender, RoutedEventArgs e)
    {
        _isGridView = false;
        GridItemsControl.Visibility = Visibility.Collapsed;
        ListViewContainer.Visibility = Visibility.Visible;
        ListViewBtn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(255, 255, 255, 255));
        GridViewBtn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }
}
