using System.IO;
using System.Linq;
using System.Windows;
using ImgViewer.Services;
using ImgViewer.ViewModels;

namespace ImgViewer;

public partial class App : Application
{
    private SingleInstanceCoordinator? _instanceCoordinator;
    private TabCommandQueue? _commandQueue;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        var imageService = new ImageService();
        var sessionService = new SessionService();
        var store = new TabStateStore();

        var viewModel = new MainViewModel(
            imageService,
            sessionService,
            store,
            Dispatcher);

        _commandQueue = viewModel.CommandQueue;

        _instanceCoordinator = new SingleInstanceCoordinator("ImgViewer");

        var isPrimary = await _instanceCoordinator.TryStartAsync(
            e.Args,
            args =>
            {
                var files = args.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (files.Count > 0)
                {
                    viewModel.Enqueue(new OpenFilesCommand(files, TabCommandSource.ExternalRequest));
                }
                viewModel.Enqueue(new ActivateWindowCommand());
                return Task.CompletedTask;
            });

        if (!isPrimary)
        {
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow
        {
            DataContext = viewModel
        };

        viewModel.Enqueue(new RestoreSessionCommand());

        await Task.Delay(100);

        if (viewModel.IsMaximized)
        {
            mainWindow.WindowState = WindowState.Maximized;
        }

        mainWindow.Show();

        var startupFiles = e.Args.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (startupFiles.Count > 0)
        {
            viewModel.Enqueue(new OpenFilesCommand(startupFiles, TabCommandSource.ExternalRequest));
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceCoordinator?.Dispose();
        base.OnExit(e);
    }
}
