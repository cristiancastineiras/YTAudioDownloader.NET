using System;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;

namespace YTAudioDownloader.Services;

public interface IYouTubeClientUtil : IDisposable
{
    ValueTask<YoutubeClient> GetAsync(CancellationToken cancellationToken = default);
}
