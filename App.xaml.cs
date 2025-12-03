using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ImgViewer.Services;
using ImgViewer.ViewModels;

namespace ImgViewer;

public partial class App : Application
{
    private SingleInstanceCoordinator? _instanceCoordinator;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        var imageService = new ImageService();
        var sessionService = new SessionService();
        var viewModel = new MainViewModel(imageService, sessionService);

        _instanceCoordinator = new SingleInstanceCoordinator("ImgViewer");

        var isPrimary = await _instanceCoordinator.TryStartAsync(
            e.Args,
            args => Dispatcher.InvokeAsync(() => HandleExternalArgumentsAsync(args, viewModel)).Task);

        if (!isPrimary)
        {
            Shutdown();
            return;
        }

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

        await HandleExternalArgumentsAsync(e.Args, viewModel);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceCoordinator?.Dispose();
        base.OnExit(e);
    }

    private static async Task HandleExternalArgumentsAsync(IEnumerable<string> args, MainViewModel viewModel)
    {
        var files = args.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            await viewModel.AddTabAsync(file);
        }

        if (Current?.MainWindow is not Window mainWindow)
        {
            return;
        }

        if (mainWindow.WindowState == WindowState.Minimized)
        {
            mainWindow.WindowState = WindowState.Normal;
        }

        mainWindow.Activate();
    }
}
