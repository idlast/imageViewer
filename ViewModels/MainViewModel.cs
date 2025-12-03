using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImgViewer.Models;
using ImgViewer.Services;
using Microsoft.Win32;

namespace ImgViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IImageService _imageService;
    private readonly ISessionService _sessionService;

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

    public MainViewModel(IImageService imageService, ISessionService sessionService)
    {
        _imageService = imageService;
        _sessionService = sessionService;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = _imageService.FileDialogFilter,
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var filePath in dialog.FileNames)
            {
                await AddTabAsync(filePath);
            }
        }
    }

    [RelayCommand]
    private void CloseTab(ImageTabViewModel? tab)
    {
        if (tab is null) return;

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (Tabs.Count > 0)
        {
            SelectedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
        }
        else
        {
            SelectedTab = null;
        }
    }

    [RelayCommand]
    private void ToggleAlwaysOnTop()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
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

    public async Task AddTabAsync(string filePath)
    {
        if (!_imageService.IsSupportedFormat(filePath))
        {
            MessageBox.Show($"サポートされていないファイル形式です: {filePath}", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existingTab = Tabs.FirstOrDefault(t => t.FilePath == filePath);
        if (existingTab is not null)
        {
            SelectedTab = existingTab;
            return;
        }

        var tab = new ImageTabViewModel(filePath, _imageService);
        Tabs.Add(tab);
        SelectedTab = tab;
        await tab.LoadImageAsync();
    }

    public async Task RestoreSessionAsync()
    {
        var session = await _sessionService.LoadSessionAsync();

        WindowWidth = session.WindowWidth;
        WindowHeight = session.WindowHeight;
        WindowLeft = session.WindowLeft;
        WindowTop = session.WindowTop;
        IsMaximized = session.IsMaximized;

        foreach (var filePath in session.OpenTabs)
        {
            await AddTabAsync(filePath);
        }

        if (session.ActiveTabIndex >= 0 && session.ActiveTabIndex < Tabs.Count)
        {
            SelectedTab = Tabs[session.ActiveTabIndex];
        }
    }

    public async Task SaveSessionAsync()
    {
        var session = new SessionData
        {
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight,
            WindowLeft = WindowLeft,
            WindowTop = WindowTop,
            IsMaximized = IsMaximized,
            OpenTabs = Tabs.Select(t => t.FilePath).ToList(),
            ActiveTabIndex = SelectedTab is not null ? Tabs.IndexOf(SelectedTab) : 0
        };

        await _sessionService.SaveSessionAsync(session);
    }

    public void ResetAllZoom()
    {
        foreach (var tab in Tabs)
        {
            tab.ResetZoom();
        }
    }

    partial void OnSelectedTabChanged(ImageTabViewModel? value)
    {
        if (value is not null && value.Image is null && !value.IsLoading)
        {
            _ = value.LoadImageAsync();
        }
    }
}
