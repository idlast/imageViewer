using System.Windows;
using ImgViewer.Services;
using ImgViewer.ViewModels;

namespace ImgViewer;

public partial class App : Application
{
    private async void OnStartup(object sender, StartupEventArgs e)
    {
        var imageService = new ImageService();
        var sessionService = new SessionService();
        var viewModel = new MainViewModel(imageService, sessionService);

        var mainWindow = new MainWindow
        {
            DataContext = viewModel
        };

        await viewModel.RestoreSessionAsync();

        if (viewModel.IsMaximized)
        {
            mainWindow.WindowState = WindowState.Maximized;
        }

        mainWindow.Show();

        // コマンドライン引数からファイルを開く（既定のアプリとして起動された場合）
        if (e.Args.Length > 0)
        {
            foreach (var filePath in e.Args)
            {
                if (System.IO.File.Exists(filePath))
                {
                    await viewModel.AddTabAsync(filePath);
                }
            }
        }
    }
}
