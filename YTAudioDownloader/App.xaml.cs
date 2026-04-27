using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace YTAudioDownloader
{
    public partial class App : Application
    {
        private static Window? _mainWindow;

        public static Window? MainAppWindow => _mainWindow;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Activate();
        }
    }
}
