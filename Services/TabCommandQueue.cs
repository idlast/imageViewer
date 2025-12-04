using System.IO;
using System.Threading.Channels;
using System.Windows;
using ImgViewer.Models;

namespace ImgViewer.Services;

public sealed class TabCommandQueue : IAsyncDisposable
{
    private static readonly string LogFilePath = Path.Combine(Path.GetTempPath(), "ImgViewer_command.log");
    private readonly Channel<TabCommand> _channel;
    private readonly TabStateStore _store;
    private readonly ISessionService _sessionService;
    private readonly IImageService _imageService;
    private readonly Func<string, Task>? _imageLoader;
    private readonly Action? _activateWindow;
    private Task? _processorTask;
    private CancellationTokenSource? _cts;

    public TabCommandQueue(
        TabStateStore store,
        ISessionService sessionService,
        IImageService imageService,
        Func<string, Task>? imageLoader = null,
        Action? activateWindow = null)
    {
        _store = store;
        _sessionService = sessionService;
        _imageService = imageService;
        _imageLoader = imageLoader;
        _activateWindow = activateWindow;
        _channel = Channel.CreateUnbounded<TabCommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        ClearLog();
        Log("CommandQueue initialized");
    }

    private static void ClearLog()
    {
        try { File.WriteAllText(LogFilePath, string.Empty); } catch { }
    }

    private static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
            File.AppendAllText(LogFilePath, line);
        }
        catch { }
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _processorTask = ProcessCommandsAsync(_cts.Token);
        Log("CommandQueue started");
    }

    public void Enqueue(TabCommand command)
    {
        Log($"Enqueue: {command}");
        _channel.Writer.TryWrite(command);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        _cts?.Cancel();

        if (_processorTask is not null)
        {
            try
            {
                await _processorTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts?.Dispose();
    }

    private async Task ProcessCommandsAsync(CancellationToken token)
    {
        Log("ProcessCommandsAsync started");
        await foreach (var command in _channel.Reader.ReadAllAsync(token))
        {
            try
            {
                Log($"Processing: {command}");
                await ExecuteCommandAsync(command, token).ConfigureAwait(false);
                Log($"Completed: {command.GetType().Name}, State: Tabs={_store.State.Tabs.Count}, Selected={_store.State.SelectedIndex}");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Log("Processing cancelled");
                break;
            }
            catch (Exception ex)
            {
                Log($"Error processing {command.GetType().Name}: {ex.Message}");
            }
        }
        Log("ProcessCommandsAsync ended");
    }

    private async Task ExecuteCommandAsync(TabCommand command, CancellationToken token)
    {
        switch (command)
        {
            case OpenFilesCommand openFiles:
                Log($"OpenFiles: {openFiles.FilePaths.Count} files, Source={openFiles.Source}");
                await HandleOpenFilesAsync(openFiles, token).ConfigureAwait(false);
                break;

            case SelectTabCommand selectTab:
                Log($"SelectTab: index={selectTab.Index}");
                _store.Apply(s => s.WithSelection(selectTab.Index));
                break;

            case SelectTabByPathCommand selectByPath:
                Log($"SelectTabByPath: {selectByPath.FilePath}");
                _store.Apply(s =>
                {
                    var index = s.Tabs.FindIndex(t => t.FilePath == selectByPath.FilePath);
                    return index >= 0 ? s.WithSelection(index) : s;
                });
                break;

            case MoveTabCommand moveTab:
                Log($"MoveTab: {moveTab.FromIndex} -> {moveTab.ToIndex}");
                _store.Apply(s => s.WithTabMoved(moveTab.FromIndex, moveTab.ToIndex));
                break;

            case CloseTabCommand closeTab:
                Log($"CloseTab: index={closeTab.Index}");
                _store.Apply(s => s.WithTabRemoved(closeTab.Index));
                break;

            case CloseTabsToRightCommand closeRight:
                Log($"CloseTabsToRight: index={closeRight.Index}");
                _store.Apply(s => s.WithTabsClosedToRight(closeRight.Index));
                break;

            case CloseOtherTabsCommand closeOthers:
                Log($"CloseOtherTabs: keep={closeOthers.KeepIndex}");
                _store.Apply(s => s.WithOnlyTab(closeOthers.KeepIndex));
                break;

            case RestoreSessionCommand:
                Log("RestoreSession");
                await HandleRestoreSessionAsync(token).ConfigureAwait(false);
                break;

            case SaveSessionCommand:
                Log("SaveSession");
                await HandleSaveSessionAsync().ConfigureAwait(false);
                break;

            case ActivateWindowCommand:
                Log("ActivateWindow");
                _activateWindow?.Invoke();
                break;
        }
    }

    private async Task HandleOpenFilesAsync(OpenFilesCommand command, CancellationToken token)
    {
        string? lastAddedPath = null;
        var shouldSelect = command.Source != TabCommandSource.SessionRestore;
        Log($"HandleOpenFiles: shouldSelect={shouldSelect}");

        foreach (var filePath in command.FilePaths)
        {
            token.ThrowIfCancellationRequested();

            if (!File.Exists(filePath))
            {
                Log($"  File not found: {filePath}");
                continue;
            }
            
            if (!_imageService.IsSupportedFormat(filePath))
            {
                Log($"  Unsupported format: {filePath}");
                continue;
            }

            var existingIndex = _store.State.Tabs.FindIndex(t => t.FilePath == filePath);
            if (existingIndex >= 0)
            {
                Log($"  Already open at index {existingIndex}: {filePath}");
                lastAddedPath = filePath;
                continue;
            }

            var tab = new TabState
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            _store.Apply(s => s.WithTab(tab, select: false));
            Log($"  Added tab: {filePath}");
            lastAddedPath = filePath;

            if (_imageLoader is not null)
            {
                Log($"  Loading image: {filePath}");
                await _imageLoader(filePath).ConfigureAwait(false);
                Log($"  Image loaded: {filePath}");
            }
        }

        if (shouldSelect && lastAddedPath is not null)
        {
            Log($"  Selecting last: {lastAddedPath}");
            _store.Apply(s =>
            {
                var index = s.Tabs.FindIndex(t => t.FilePath == lastAddedPath);
                Log($"  Selection index: {index}");
                return index >= 0 ? s.WithSelection(index) : s;
            });
        }
        Log($"HandleOpenFiles done. Tabs={_store.State.Tabs.Count}, Selected={_store.State.SelectedIndex}");
    }

    private async Task HandleRestoreSessionAsync(CancellationToken token)
    {
        Log("HandleRestoreSession start");
        var session = await _sessionService.LoadSessionAsync().ConfigureAwait(false);
        Log($"Session loaded: {session.OpenTabs.Count} tabs, activeIndex={session.ActiveTabIndex}");

        _store.Apply(s => s with
        {
            WindowWidth = session.WindowWidth,
            WindowHeight = session.WindowHeight,
            WindowLeft = session.WindowLeft,
            WindowTop = session.WindowTop,
            IsMaximized = session.IsMaximized,
            ZoomStepPercent = session.ZoomStepPercent
        }, isSessionRestore: true);

        if (session.OpenTabs.Count > 0)
        {
            await HandleOpenFilesAsync(
                new OpenFilesCommand(session.OpenTabs, TabCommandSource.SessionRestore),
                token).ConfigureAwait(false);

            if (session.ActiveTabIndex >= 0 && session.ActiveTabIndex < _store.State.Tabs.Count)
            {
                Log($"Restoring selection to index {session.ActiveTabIndex}");
                _store.Apply(s => s.WithSelection(session.ActiveTabIndex));
            }
        }
        Log($"HandleRestoreSession done. Tabs={_store.State.Tabs.Count}, Selected={_store.State.SelectedIndex}");
    }

    private async Task HandleSaveSessionAsync()
    {
        var state = _store.State;
        Log($"SaveSession: Tabs={state.Tabs.Count}, Selected={state.SelectedIndex}");
        var session = new SessionData
        {
            WindowWidth = state.WindowWidth,
            WindowHeight = state.WindowHeight,
            WindowLeft = state.WindowLeft,
            WindowTop = state.WindowTop,
            IsMaximized = state.IsMaximized,
            OpenTabs = state.Tabs.Select(t => t.FilePath).ToList(),
            ActiveTabIndex = state.SelectedIndex >= 0 ? state.SelectedIndex : 0,
            ZoomStepPercent = state.ZoomStepPercent
        };

        await _sessionService.SaveSessionAsync(session).ConfigureAwait(false);
    }
}
