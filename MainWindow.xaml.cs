using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ImgViewer.Services;
using ImgViewer.ViewModels;

namespace ImgViewer;

public partial class MainWindow : Window
{
    private static readonly string UiLogPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ImgViewer_ui.log");
    private const double DragScrollEdgeThreshold = 36;
    private const double DragScrollStep = 18;
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private TabPanel? _tabStripPanel;
    private ScrollViewer? _tabHeaderScrollViewer;
    private TabItem? _tabDragSourceItem;
    private ImageTabViewModel? _tabDragSourceViewModel;
    private TranslateTransform? _tabDragTransform;
    private bool _isTabDragActive;
    private Point _tabDragStartPointPanel;
    private double _tabDragBaseLeft;
    private int _tabDragOriginalIndex;
    private int _tabDragCurrentIndex;
    private bool _suppressSelectionChanged;
    
    private TabItem? _pendingDragItem;
    private ImageTabViewModel? _pendingDragViewModel;
    private Point _pendingDragStartPoint;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        try
        {
            System.IO.File.WriteAllText(UiLogPath, string.Empty);
        }
        catch
        {
        }
    }

    private static void LogUi(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
            System.IO.File.AppendAllText(UiLogPath, line);
        }
        catch
        {
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }
        if (e.NewValue is MainViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedTab))
        {
            LogUi($"PropertyChanged -> SelectedTab={ViewModel.SelectedTab?.FileName ?? "<null>"}");
            SyncTabControlSelection();
        }
    }

    private void SyncTabControlSelection()
    {
        var selectedTab = ViewModel.SelectedTab;
        if (selectedTab is null)
        {
            LogUi("SyncTabControlSelection: SelectedTab is null");
            return;
        }

        var index = ViewModel.Tabs.IndexOf(selectedTab);
        LogUi($"SyncTabControlSelection: target index={index}, current index={MainTabControl.SelectedIndex}");
        if (index >= 0 && MainTabControl.SelectedIndex != index)
        {
            try
            {
                _suppressSelectionChanged = true;
                MainTabControl.SelectedIndex = index;
                LogUi($"  -> Programmatic select index {index}");
                EnsureTabContainerVisible(index);
            }
            finally
            {
                _suppressSelectionChanged = false;
            }
        }
        else if (index >= 0)
        {
            EnsureTabContainerVisible(index);
        }
    }

    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = MainTabControl.SelectedItem as ImageTabViewModel;
        LogUi($"OnTabSelectionChanged: index={MainTabControl.SelectedIndex}, tab={vm?.FileName ?? "<null>"}, suppress={_suppressSelectionChanged}");
        if (!_suppressSelectionChanged)
        {
            EnsureTabContainerVisible(MainTabControl.SelectedIndex);
        }
    }

    private void EnsureTabContainerVisible(int index)
    {
        if (index < 0)
        {
            return;
        }

        EnsureTabStripPanel();

        MainTabControl.UpdateLayout();
        if (MainTabControl.ItemContainerGenerator.ContainerFromIndex(index) is TabItem tab)
        {
            LogUi($"EnsureTabContainerVisible: focusing tab {tab.Header ?? tab.Content}" );
            if (!tab.IsFocused)
            {
                tab.Focus();
            }
            tab.BringIntoView();
            EnsureTabIsWithinScrollViewport(tab);
        }
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        ViewModel.Enqueue(new OpenFilesCommand(files, TabCommandSource.UserAction));
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnTabPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TabItem tabItem)
        {
            return;
        }

        if (IsCloseButton(e.OriginalSource as DependencyObject))
        {
            ClearPendingDrag();
            return;
        }

        if (!EnsureTabStripPanel())
        {
            return;
        }

        _pendingDragItem = tabItem;
        _pendingDragViewModel = tabItem.DataContext as ImageTabViewModel;
        _pendingDragStartPoint = e.GetPosition(_tabStripPanel);
    }

    private void ClearPendingDrag()
    {
        _pendingDragItem = null;
        _pendingDragViewModel = null;
        _pendingDragStartPoint = default;
    }

    private void OnTabPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            if (_isTabDragActive)
            {
                CompleteTabDrag();
            }
            ClearPendingDrag();
            return;
        }

        if (_isTabDragActive)
        {
            if (_tabStripPanel is null || _tabDragTransform is null || _tabDragSourceItem is null)
            {
                return;
            }
            UpdateTabDrag(e.GetPosition(_tabStripPanel));
            return;
        }

        if (_pendingDragItem is null || _pendingDragViewModel is null || _tabStripPanel is null)
        {
            return;
        }

        var pointer = e.GetPosition(_tabStripPanel);
        var delta = pointer - _pendingDragStartPoint;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _tabDragSourceItem = _pendingDragItem;
        _tabDragSourceViewModel = _pendingDragViewModel;
        _tabDragTransform = EnsureTranslateTransform(_tabDragSourceItem);
        _tabDragTransform.BeginAnimation(TranslateTransform.XProperty, null);
        _tabDragTransform.X = 0;
        _tabDragStartPointPanel = _pendingDragStartPoint;
        _tabDragBaseLeft = _tabDragSourceItem.TranslatePoint(new Point(0, 0), _tabStripPanel).X;
        
        ClearPendingDrag();
        BeginTabDrag(pointer);
    }

    private void OnTabPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ClearPendingDrag();
        if (_isTabDragActive)
        {
            CompleteTabDrag();
        }
    }

    private void OnTabLostMouseCapture(object sender, MouseEventArgs e)
    {
        ClearPendingDrag();
        if (_isTabDragActive)
        {
            CompleteTabDrag();
        }
    }

    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        ViewModel.SaveSession();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.ResetAllZoom();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BeginTabDrag(Point pointerPosition)
    {
        if (_tabStripPanel is null || _tabDragSourceItem is null || _tabDragSourceViewModel is null)
        {
            return;
        }

        _tabDragSourceItem.CaptureMouse();
        Panel.SetZIndex(_tabDragSourceItem, 1000);
        _tabDragSourceItem.Opacity = 0.9;
        _tabDragBaseLeft = _tabDragSourceItem.TranslatePoint(new Point(0, 0), _tabStripPanel).X;
        _tabDragOriginalIndex = _tabDragCurrentIndex = ViewModel.Tabs.IndexOf(_tabDragSourceViewModel);
        _tabDragStartPointPanel = pointerPosition;
        _isTabDragActive = true;
    }

    private void UpdateTabDrag(Point pointerPosition)
    {
        if (_tabStripPanel is null || _tabDragTransform is null || _tabDragSourceItem is null)
        {
            return;
        }

        var deltaX = pointerPosition.X - _tabDragStartPointPanel.X;
        _tabDragTransform.X = deltaX;

        var dragCenter = _tabDragBaseLeft + deltaX + (_tabDragSourceItem.ActualWidth / 2);

        if (deltaX > 0)
        {
            TrySwapWithNeighbor(_tabDragCurrentIndex + 1, dragCenter, pointerPosition, movingRight: true);
        }
        else if (deltaX < 0)
        {
            TrySwapWithNeighbor(_tabDragCurrentIndex - 1, dragCenter, pointerPosition, movingRight: false);
        }

        AutoScrollTabStrip(pointerPosition);
    }

    private void TrySwapWithNeighbor(int neighborIndex, double dragCenter, Point pointerPosition, bool movingRight)
    {
        if (_tabStripPanel is null)
        {
            return;
        }

        if (neighborIndex < 0 || neighborIndex >= ViewModel.Tabs.Count)
        {
            return;
        }

        var neighborItem = GetTabItemAt(neighborIndex);
        if (neighborItem is null)
        {
            return;
        }

        var neighborLeft = neighborItem.TranslatePoint(new Point(0, 0), _tabStripPanel).X;
        var neighborCenter = neighborLeft + neighborItem.ActualWidth / 2;
        var crossed = movingRight ? dragCenter > neighborCenter : dragCenter < neighborCenter;

        if (!crossed)
        {
            return;
        }

        ViewModel.MoveTab(_tabDragCurrentIndex, neighborIndex);
        _tabDragCurrentIndex = neighborIndex;
        AnimateNeighborSwap(neighborItem, movingRight ? neighborItem.ActualWidth : -neighborItem.ActualWidth);
        ResetDragReference(pointerPosition);
    }

    private void ResetDragReference(Point pointerPosition)
    {
        if (_tabStripPanel is null || _tabDragSourceItem is null || _tabDragTransform is null)
        {
            return;
        }

        _tabDragBaseLeft = _tabDragSourceItem.TranslatePoint(new Point(0, 0), _tabStripPanel).X;
        _tabDragStartPointPanel = pointerPosition;
        _tabDragTransform.X = 0;
    }

    private void CompleteTabDrag()
    {
        if (_tabDragSourceItem is null)
        {
            ResetTabDragState();
            return;
        }

        if (_tabDragTransform is not null)
        {
            var animation = new DoubleAnimation(_tabDragTransform.X, 0, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            _tabDragTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        Panel.SetZIndex(_tabDragSourceItem, 0);
        _tabDragSourceItem.Opacity = 1.0;

        if (_tabDragSourceItem.IsMouseCaptured)
        {
            _tabDragSourceItem.ReleaseMouseCapture();
        }

        ResetTabDragState();
    }

    private void ResetTabDragState()
    {
        _isTabDragActive = false;
        _tabDragSourceItem = null;
        _tabDragSourceViewModel = null;
        _tabDragTransform = null;
        _tabDragStartPointPanel = default;
        _tabDragBaseLeft = 0;
        _tabDragOriginalIndex = 0;
        _tabDragCurrentIndex = 0;
    }

    private bool EnsureTabStripPanel()
    {
        if (_tabStripPanel is null)
        {
            _tabStripPanel = FindVisualChild<TabPanel>(MainTabControl);
        }

        if (_tabHeaderScrollViewer is null)
        {
            MainTabControl.ApplyTemplate();
            _tabHeaderScrollViewer = MainTabControl.Template.FindName("HeaderScrollViewer", MainTabControl) as ScrollViewer;
        }

        return _tabStripPanel is not null;
    }

    private TabItem? GetTabItemAt(int index)
    {
        return MainTabControl.ItemContainerGenerator.ContainerFromIndex(index) as TabItem;
    }

    private static void AnimateNeighborSwap(TabItem neighborItem, double fromOffset)
    {
        var transform = EnsureTranslateTransform(neighborItem);
        transform.BeginAnimation(TranslateTransform.XProperty, null);
        transform.X = fromOffset;

        var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(140))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        transform.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private static bool IsCloseButton(DependencyObject? source)
    {
        return FindAncestor<ButtonBase>(source) is not null;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent is null)
        {
            return null;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static TranslateTransform EnsureTranslateTransform(TabItem tabItem)
    {
        if (tabItem.RenderTransform is TranslateTransform existing && !existing.IsFrozen)
        {
            return existing;
        }

        var transform = new TranslateTransform();
        tabItem.RenderTransform = transform;
        return transform;
    }

    private void EnsureTabIsWithinScrollViewport(TabItem tab)
    {
        if (_tabStripPanel is null || _tabHeaderScrollViewer is null)
        {
            return;
        }

        var tabLeft = tab.TranslatePoint(new Point(0, 0), _tabStripPanel).X;
        var tabRight = tabLeft + tab.ActualWidth;
        var offset = _tabHeaderScrollViewer.HorizontalOffset;
        var viewport = _tabHeaderScrollViewer.ViewportWidth;

        if (viewport <= 0)
        {
            return;
        }

        if (tabLeft < offset)
        {
            _tabHeaderScrollViewer.ScrollToHorizontalOffset(tabLeft);
        }
        else if (tabRight > offset + viewport)
        {
            _tabHeaderScrollViewer.ScrollToHorizontalOffset(tabRight - viewport);
        }
    }

    private void AutoScrollTabStrip(Point pointerPosition)
    {
        if (_tabStripPanel is null || _tabHeaderScrollViewer is null)
        {
            return;
        }

        var pointerInViewer = _tabStripPanel.TranslatePoint(pointerPosition, _tabHeaderScrollViewer);
        var viewportWidth = _tabHeaderScrollViewer.ViewportWidth;

        if (viewportWidth <= 0)
        {
            return;
        }

        var horizontalOffset = _tabHeaderScrollViewer.HorizontalOffset;
        var maxOffset = _tabHeaderScrollViewer.ScrollableWidth;
        var newOffset = horizontalOffset;
        var shouldScroll = false;

        if (pointerInViewer.X < DragScrollEdgeThreshold && horizontalOffset > 0)
        {
            var delta = Math.Min(DragScrollStep, DragScrollEdgeThreshold - pointerInViewer.X);
            newOffset = Math.Max(0, horizontalOffset - delta);
            shouldScroll = true;
        }
        else if (pointerInViewer.X > viewportWidth - DragScrollEdgeThreshold && horizontalOffset < maxOffset)
        {
            var delta = Math.Min(DragScrollStep, pointerInViewer.X - (viewportWidth - DragScrollEdgeThreshold));
            newOffset = Math.Min(maxOffset, horizontalOffset + delta);
            shouldScroll = true;
        }

        if (shouldScroll)
        {
            _tabHeaderScrollViewer.ScrollToHorizontalOffset(newOffset);
        }
    }
}
