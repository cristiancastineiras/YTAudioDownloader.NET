using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using YTAudioDownloader.Models;
using YTAudioDownloader.State;

namespace YTAudioDownloader.Pages;

public sealed partial class QueuePage : Page
{
    public QueuePage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        AppState.Instance.Downloads.CollectionChanged += OnDownloadsChanged;
        RefreshLists();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        AppState.Instance.Downloads.CollectionChanged -= OnDownloadsChanged;
    }

    private void OnDownloadsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(RefreshLists);
    }

    private void RefreshLists()
    {
        var all = AppState.Instance.Downloads.ToList();
        var active = all.Where(d => d.IsActive || d.IsError).ToList();
        var completed = all.Where(d => d.IsCompleted).ToList();

        ActiveList.ItemsSource = active;
        CompletedList.ItemsSource = completed;

        ActiveHeader.Visibility = active.Count > 0
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;

        CompletedHeader.Visibility = completed.Count > 0
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;

        EmptyState.Visibility = all.Count == 0
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;

        var activeCount = active.Count;
        var completedCount = completed.Count;
        SubtitleBlock.Text = activeCount > 0
            ? $"{activeCount} activa{(activeCount != 1 ? "s" : "")} · {completedCount} completada{(completedCount != 1 ? "s" : "")}"
            : $"{completedCount} descarga{(completedCount != 1 ? "s" : "")} completada{(completedCount != 1 ? "s" : "")}";
    }
}
