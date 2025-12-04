using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ImgViewer.ViewModels;

namespace ImgViewer;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private TabPanel? _tabStripPanel;
    private TabItem? _tabDragSourceItem;
    private ImageTabViewModel? _tabDragSourceViewModel;
    private TranslateTransform? _tabDragTransform;
    private bool _isTabDragActive;
    private Point _tabDragStartPointPanel;
    private double _tabDragBaseLeft;
    private int _tabDragOriginalIndex;
    private int _tabDragCurrentIndex;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        foreach (var file in files)
        {
            await ViewModel.AddTabAsync(file);
        }
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
            ResetTabDragState();
            return;
        }

        if (!EnsureTabStripPanel())
        {
            return;
        }

        _tabDragSourceItem = tabItem;
        _tabDragSourceViewModel = tabItem.DataContext as ImageTabViewModel;
        _tabDragTransform = EnsureTranslateTransform(tabItem);
        _tabDragTransform.BeginAnimation(TranslateTransform.XProperty, null);
        _tabDragTransform.X = 0;

        _tabDragStartPointPanel = e.GetPosition(_tabStripPanel);
        _tabDragBaseLeft = tabItem.TranslatePoint(new Point(0, 0), _tabStripPanel).X;
        _isTabDragActive = false;
    }

    private void OnTabPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_tabStripPanel is null || _tabDragSourceItem is null || _tabDragSourceViewModel is null)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            CompleteTabDrag();
            return;
        }

        var pointer = e.GetPosition(_tabStripPanel);

        if (!_isTabDragActive)
        {
            var delta = pointer - _tabDragStartPointPanel;
            if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            BeginTabDrag(pointer);
        }

        if (!_isTabDragActive)
        {
            return;
        }

        UpdateTabDrag(pointer);
    }

    private void OnTabPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        CompleteTabDrag();
    }

    private void OnTabLostMouseCapture(object sender, MouseEventArgs e)
    {
        CompleteTabDrag();
    }

    private async void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        await ViewModel.SaveSessionAsync();
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
        if (_tabStripPanel is not null)
        {
            return true;
        }

        _tabStripPanel = FindVisualChild<TabPanel>(MainTabControl);
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

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldViewModel)
        {
            oldViewModel.TabMoved -= OnTabMoved;
        }

        if (e.NewValue is MainViewModel newViewModel)
        {
            newViewModel.TabMoved += OnTabMoved;
        }
    }

    private void OnTabMoved(object? sender, TabMovedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        viewModel.SelectedTab = e.Tab;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.TabMoved -= OnTabMoved;
        }

        base.OnClosed(e);
    }
}