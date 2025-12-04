using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImgViewer.Services;
using Microsoft.Win32;

namespace ImgViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly string LogFilePath = Path.Combine(Path.GetTempPath(), "ImgViewer_viewmodel.log");
    private readonly IImageService _imageService;
    private readonly TabStateStore _store;
    private readonly TabCommandQueue _commandQueue;
    private readonly Dispatcher _dispatcher;
    private readonly Dictionary<string, ImageTabViewModel> _tabViewModels = new();
    private bool _isApplyingStateSelection;
    private DateTime _selectionEnforcementUntil = DateTime.MinValue;
    private string? _lastStateSelectionPath;

    private static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
            File.AppendAllText(LogFilePath, line);
        }
        catch { }
    }

    public ObservableCollection<ImageTabViewModel> Tabs { get; } = [];

    [ObservableProperty]
    private ImageTabViewModel? _selectedTab;

    [ObservableProperty]
    private bool _isAlwaysOnTop;

    [ObservableProperty]
    private double _windowWidth = 800;

    [ObservableProperty]
    private double _windowHeight = 450;

    [ObservableProperty]
    private double _windowLeft = 100;

    [ObservableProperty]
    private double _windowTop = 100;

    [ObservableProperty]
    private bool _isMaximized;

    [ObservableProperty]
    private int _zoomStepPercent = 4;

    public TabCommandQueue CommandQueue => _commandQueue;

    public MainViewModel(
        IImageService imageService,
        ISessionService sessionService,
        TabStateStore store,
        Dispatcher dispatcher)
    {
        _imageService = imageService;
        _store = store;
        _dispatcher = dispatcher;

        _commandQueue = new TabCommandQueue(
            store,
            sessionService,
            imageService,
            imageLoader: LoadImageForTabAsync,
            activateWindow: ActivateWindow);

        _store.StateChanged += OnStateChanged;
        _commandQueue.Start();
    }

    public void Enqueue(TabCommand command)
    {
        _commandQueue.Enqueue(command);
    }

    [RelayCommand]
    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = _imageService.FileDialogFilter,
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            Enqueue(new OpenFilesCommand(dialog.FileNames, TabCommandSource.UserAction));
        }
    }

    [RelayCommand]
    private void CloseTab(ImageTabViewModel? tab)
    {
        if (tab is null) return;
        var index = Tabs.IndexOf(tab);
        if (index >= 0)
        {
            Enqueue(new CloseTabCommand(index));
        }
    }

    [RelayCommand(CanExecute = nameof(CanCloseTabsToTheRight))]
    private void CloseTabsToTheRight(ImageTabViewModel? tab)
    {
        if (tab is null) return;
        var index = Tabs.IndexOf(tab);
        if (index >= 0)
        {
            Enqueue(new CloseTabsToRightCommand(index));
        }
    }

    private bool CanCloseTabsToTheRight(ImageTabViewModel? tab)
    {
        if (tab is null) return false;
        var index = Tabs.IndexOf(tab);
        return index >= 0 && index < Tabs.Count - 1;
    }

    [RelayCommand(CanExecute = nameof(CanCloseOtherTabs))]
    private void CloseOtherTabs(ImageTabViewModel? tab)
    {
        if (tab is null) return;
        var index = Tabs.IndexOf(tab);
        if (index >= 0)
        {
            Enqueue(new CloseOtherTabsCommand(index));
        }
    }

    private bool CanCloseOtherTabs(ImageTabViewModel? tab)
    {
        return tab is not null && Tabs.Count > 1;
    }

    [RelayCommand]
    private void ToggleAlwaysOnTop()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
    }

    [RelayCommand]
    private void SetZoomStep(object? parameter)
    {
        var target = NormalizeZoomStep(ParseZoomParameter(parameter));
        if (target != ZoomStepPercent)
        {
            ZoomStepPercent = target;
        }
    }

    [RelayCommand]
    private void ZoomIn()
    {
        SelectedTab?.ZoomIn();
    }

    [RelayCommand]
    private void ZoomOut()
    {
        SelectedTab?.ZoomOut();
    }

    [RelayCommand]
    private void ResetZoom()
    {
        SelectedTab?.ResetZoom();
    }

    public void MoveTab(int fromIndex, int toIndex)
    {
        Enqueue(new MoveTabCommand(fromIndex, toIndex));
    }

    public void ResetAllZoom()
    {
        foreach (var tab in Tabs)
        {
            tab.ResetZoom();
        }
    }

    public void SaveSession()
    {
        var state = _store.State;
        _store.Apply(s => s with
        {
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight,
            WindowLeft = WindowLeft,
            WindowTop = WindowTop,
            IsMaximized = IsMaximized,
            ZoomStepPercent = ZoomStepPercent
        });
        Enqueue(new SaveSessionCommand());
    }

    partial void OnSelectedTabChanged(ImageTabViewModel? value)
    {
        var tabName = value?.FileName ?? "<null>";
        Log($"OnSelectedTabChanged: value={tabName}, applyingState={_isApplyingStateSelection}");
        if (value is not null && value.Image is null && !value.IsLoading)
        {
            _ = value.LoadImageAsync();
        }

        if (_isApplyingStateSelection || value is null)
        {
            Log("  Ignored (state sync or null)");
            return;
        }

        var now = DateTime.UtcNow;
        var stateSelectionPath = _store.State.SelectedFilePath;
        if (now <= _selectionEnforcementUntil && stateSelectionPath is not null && value.FilePath != stateSelectionPath)
        {
            Log($"  Enforcing state selection ({stateSelectionPath}) during stabilization window");
            ForceSelectStateTab();
            return;
        }

        _selectionEnforcementUntil = DateTime.MinValue;
        _lastStateSelectionPath = value.FilePath;

        var index = Tabs.IndexOf(value);
        if (index >= 0)
        {
            Log($"  Enqueue SelectTabCommand index={index}");
            Enqueue(new SelectTabCommand(index));
        }
        else
        {
            Log("  Tab not found in Tabs collection");
        }
    }

    partial void OnZoomStepPercentChanged(int value)
    {
        var normalized = NormalizeZoomStep(value);
        if (normalized != value)
        {
            ZoomStepPercent = normalized;
            return;
        }

        foreach (var tab in Tabs)
        {
            tab.ZoomStepPercent = normalized;
        }
    }

    private void OnStateChanged(object? sender, StateChangedEventArgs e)
    {
        Log($"OnStateChanged: Tabs={e.NewState.Tabs.Count}, Selected={e.NewState.SelectedIndex}");
        _dispatcher.Invoke(() => SyncFromState(e.NewState));
    }

    private void SyncFromState(AppState state)
    {
        Log($"SyncFromState START: state.Tabs={state.Tabs.Count}, state.Selected={state.SelectedIndex}, UI.Tabs={Tabs.Count}");
        
        WindowWidth = state.WindowWidth;
        WindowHeight = state.WindowHeight;
        WindowLeft = state.WindowLeft;
        WindowTop = state.WindowTop;
        IsMaximized = state.IsMaximized;

        var currentPaths = Tabs.Select(t => t.FilePath).ToHashSet();
        var statePaths = state.Tabs.Select(t => t.FilePath).ToHashSet();

        foreach (var tab in Tabs.ToList())
        {
            if (!statePaths.Contains(tab.FilePath))
            {
                Tabs.Remove(tab);
                _tabViewModels.Remove(tab.FilePath);
            }
        }

        for (var i = 0; i < state.Tabs.Count; i++)
        {
            var tabState = state.Tabs[i];
            if (!_tabViewModels.TryGetValue(tabState.FilePath, out var vm))
            {
                vm = new ImageTabViewModel(tabState.FilePath, _imageService)
                {
                    ZoomStepPercent = ZoomStepPercent
                };
                _tabViewModels[tabState.FilePath] = vm;
            }

            var currentIndex = Tabs.IndexOf(vm);
            if (currentIndex < 0)
            {
                if (i < Tabs.Count)
                {
                    Log($"  Insert tab at {i}: {tabState.FileName}");
                    Tabs.Insert(i, vm);
                }
                else
                {
                    Log($"  Add tab: {tabState.FileName}");
                    Tabs.Add(vm);
                }
            }
            else if (currentIndex != i)
            {
                Log($"  Move tab {currentIndex} -> {i}: {tabState.FileName}");
                Tabs.Move(currentIndex, i);
            }
        }

        Log($"  Before selection: UI.Tabs={Tabs.Count}, state.SelectedIndex={state.SelectedIndex}");
        _isApplyingStateSelection = true;
        try
        {
            if (state.SelectedIndex >= 0 && state.SelectedIndex < Tabs.Count)
            {
                var targetTab = Tabs[state.SelectedIndex];
                Log($"  Setting SelectedTab to index {state.SelectedIndex}: {targetTab.FileName}");
                
                if (SelectedTab != targetTab)
                {
                    SelectedTab = null;
                    SelectedTab = targetTab;
                }
                
                Log($"  SelectedTab is now: {SelectedTab?.FileName}");
                _lastStateSelectionPath = targetTab.FilePath;
                _selectionEnforcementUntil = DateTime.UtcNow.AddMilliseconds(250);
            }
            else
            {
                Log($"  Selection index out of range, selecting first");
                SelectedTab = Tabs.FirstOrDefault();
                _lastStateSelectionPath = SelectedTab?.FilePath;
                _selectionEnforcementUntil = DateTime.MinValue;
            }
        }
        finally
        {
            _isApplyingStateSelection = false;
        }

        Log($"SyncFromState END: UI.Tabs={Tabs.Count}, SelectedTab={SelectedTab?.FileName}");
        CloseTabsToTheRightCommand.NotifyCanExecuteChanged();
        CloseOtherTabsCommand.NotifyCanExecuteChanged();
    }

    private void ForceSelectStateTab()
    {
        var state = _store.State;
        if (state.SelectedIndex < 0 || state.SelectedIndex >= Tabs.Count)
        {
            return;
        }

        var target = Tabs[state.SelectedIndex];
        _isApplyingStateSelection = true;
        try
        {
            if (SelectedTab != target)
            {
                SelectedTab = null;
                SelectedTab = target;
            }
            Log($"  Forced SelectedTab to state value: {target.FileName}");
        }
        finally
        {
            _isApplyingStateSelection = false;
        }
    }

    private async Task LoadImageForTabAsync(string filePath)
    {
        await _dispatcher.InvokeAsync(async () =>
        {
            if (_tabViewModels.TryGetValue(filePath, out var vm))
            {
                await vm.LoadImageAsync();
            }
        });
    }

    private void ActivateWindow()
    {
        _dispatcher.InvokeAsync(() =>
        {
            if (Application.Current?.MainWindow is Window mainWindow)
            {
                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                }
                mainWindow.Activate();
            }
        });
    }

    private static int ParseZoomParameter(object? parameter)
    {
        return parameter switch
        {
            int value => value,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 4
        };
    }

    private static int NormalizeZoomStep(int value)
    {
        return value switch
        {
            10 => 10,
            20 => 20,
            _ => 4
        };
    }
}
