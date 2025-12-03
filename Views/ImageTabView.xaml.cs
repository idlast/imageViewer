using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ImgViewer.ViewModels;

namespace ImgViewer.Views;

public partial class ImageTabView : UserControl
{
    private readonly record struct ZoomAnchor(double ImageX, double ImageY, double ViewportRatioX, double ViewportRatioY);
    private readonly record struct FitDisplayInfo(double Scale, double OffsetX, double OffsetY, double DisplayWidth, double DisplayHeight);
    private bool _isDragging;
    private Point _dragStartPoint;
    private Point _scrollOrigin;
    private bool _isUpdatingFromView;
    private ImageTabViewModel? _currentViewModel;
    private const double ZoomMinLevel = 0.1;
    private const double ZoomMaxLevel = 10.0;
    private static readonly TimeSpan ZoomAnimationDuration = TimeSpan.FromMilliseconds(600);
    private DoubleAnimation? _activeZoomAnimation;
    private AnimationClock? _activeZoomClock;
    private double _zoomAnimationFrom;
    private double _zoomAnimationTo;
    private ZoomAnchor? _activeZoomAnchor;

    public ImageTabView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        if (DataContext is not ImageTabViewModel vm) return;
        if (e.Delta == 0) return;

        var wasZoomed = vm.IsZoomed;
        var currentZoom = vm.ZoomLevel;

        if (!wasZoomed)
        {
            var fitZoom = CalculateFitZoomLevel(vm);
            if (Math.Abs(fitZoom - currentZoom) > 0.0001)
            {
                currentZoom = fitZoom;
                vm.ZoomLevel = fitZoom;
            }
        }

        _activeZoomAnchor = null;
        TryCaptureZoomAnchor(e, vm, currentZoom, wasZoomed);

        var stepCount = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
        if (Math.Abs(stepCount) < double.Epsilon) return;

        var baseFactor = GetZoomStepFactor(vm);
        var factor = Math.Pow(baseFactor, stepCount);
        var targetZoom = Math.Clamp(currentZoom * factor, ZoomMinLevel, ZoomMaxLevel);
        if (Math.Abs(targetZoom - currentZoom) < 0.0001) return;

        if (!wasZoomed)
        {
            vm.IsZoomed = true;
        }

        StartZoomAnimation(vm, targetZoom);

        e.Handled = true;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        StopZoomAnimation();
        if (e.OldValue is ImageTabViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is ImageTabViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            _currentViewModel = newVm;
            Dispatcher.BeginInvoke(new Action(() => SyncScrollOffsets(newVm)));
        }
        else
        {
            _currentViewModel = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ImageTabViewModel vm) return;

        if (e.PropertyName == nameof(ImageTabViewModel.IsZoomed))
        {
            if (!vm.IsZoomed)
            {
                Dispatcher.Invoke(() =>
                {
                    StopZoomAnimation();
                    if (_isDragging)
                    {
                        EndDrag();
                    }
                });
            }
        }

        if (_isUpdatingFromView) return;

        if (e.PropertyName == nameof(ImageTabViewModel.ScrollOffsetX) ||
            e.PropertyName == nameof(ImageTabViewModel.ScrollOffsetY))
        {
            Dispatcher.Invoke(() => SyncScrollOffsets(vm));
        }
    }

    private void SyncScrollOffsets(ImageTabViewModel vm)
    {
        if (ZoomScrollViewer is null) return;
        ZoomScrollViewer.ScrollToHorizontalOffset(vm.ScrollOffsetX);
        ZoomScrollViewer.ScrollToVerticalOffset(vm.ScrollOffsetY);
    }

    private void OnScrollViewerMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ImageTabViewModel vm || !vm.IsZoomed || ZoomScrollViewer is null)
        {
            return;
        }

        _isDragging = true;
        _dragStartPoint = e.GetPosition(this);
        _scrollOrigin = new Point(ZoomScrollViewer.HorizontalOffset, ZoomScrollViewer.VerticalOffset);
        ZoomScrollViewer.CaptureMouse();
        Mouse.OverrideCursor = Cursors.Hand;
        e.Handled = true;
    }

    private void OnScrollViewerMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || ZoomScrollViewer is null || _currentViewModel is null)
        {
            return;
        }

        var current = e.GetPosition(this);
        var delta = current - _dragStartPoint;
        var targetX = _scrollOrigin.X - delta.X;
        var targetY = _scrollOrigin.Y - delta.Y;

        ZoomScrollViewer.ScrollToHorizontalOffset(targetX);
        ZoomScrollViewer.ScrollToVerticalOffset(targetY);

        _isUpdatingFromView = true;
        _currentViewModel.UpdateScrollOffsets(ZoomScrollViewer.HorizontalOffset, ZoomScrollViewer.VerticalOffset);
        _isUpdatingFromView = false;
    }

    private void OnScrollViewerMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        EndDrag();
        e.Handled = true;
    }

    private void OnScrollViewerMouseLeave(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        EndDrag();
    }

    private void EndDrag()
    {
        _isDragging = false;
        ZoomScrollViewer?.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
    }

    private void OnFitToWindowClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ImageTabViewModel vm) return;
        StopZoomAnimation();
        vm.ResetZoom();
        e.Handled = true;
    }

    private bool TryCaptureZoomAnchor(MouseEventArgs e, ImageTabViewModel vm, double currentZoom, bool wasZoomed)
    {
        if (ZoomScrollViewer is not null && (wasZoomed || vm.IsZoomed))
        {
            var viewportWidth = ZoomScrollViewer.ViewportWidth;
            var viewportHeight = ZoomScrollViewer.ViewportHeight;
            if (viewportWidth <= 0 && RootHost is not null)
            {
                viewportWidth = RootHost.ActualWidth;
                viewportHeight = RootHost.ActualHeight;
            }

            if (viewportWidth > 0 && viewportHeight > 0)
            {
                var cursor = e.GetPosition(ZoomScrollViewer);
                var anchor = new ZoomAnchor(
                    (ZoomScrollViewer.HorizontalOffset + cursor.X) / currentZoom,
                    (ZoomScrollViewer.VerticalOffset + cursor.Y) / currentZoom,
                    ClampRatio(cursor.X / viewportWidth),
                    ClampRatio(cursor.Y / viewportHeight));
                _activeZoomAnchor = anchor;
                return true;
            }
        }

        var fitInfo = GetFitDisplayInfo(vm);
        if (fitInfo is null || FitViewbox is null || RootHost is null)
        {
            return false;
        }

        var cursorInFit = e.GetPosition(FitViewbox);
        var cursorInHost = e.GetPosition(RootHost);
        var clampedX = Math.Clamp(cursorInFit.X - fitInfo.Value.OffsetX, 0, fitInfo.Value.DisplayWidth);
        var clampedY = Math.Clamp(cursorInFit.Y - fitInfo.Value.OffsetY, 0, fitInfo.Value.DisplayHeight);
        var imageX = clampedX / fitInfo.Value.Scale;
        var imageY = clampedY / fitInfo.Value.Scale;

        var hostWidth = RootHost.ActualWidth;
        var hostHeight = RootHost.ActualHeight;
        var ratioX = hostWidth > 0 ? ClampRatio(cursorInHost.X / hostWidth) : 0.5;
        var ratioY = hostHeight > 0 ? ClampRatio(cursorInHost.Y / hostHeight) : 0.5;

        _activeZoomAnchor = new ZoomAnchor(imageX, imageY, ratioX, ratioY);
        return true;
    }

    private void ApplyZoomAnchor(ImageTabViewModel vm, double currentZoom)
    {
        if (_activeZoomAnchor is null) return;

        var anchor = _activeZoomAnchor.Value;
        double viewportWidth = 0;
        double viewportHeight = 0;

        if (ZoomScrollViewer is not null)
        {
            viewportWidth = ZoomScrollViewer.ViewportWidth;
            viewportHeight = ZoomScrollViewer.ViewportHeight;
        }

        if ((viewportWidth <= 0 || viewportHeight <= 0) && RootHost is not null)
        {
            viewportWidth = viewportWidth <= 0 ? RootHost.ActualWidth : viewportWidth;
            viewportHeight = viewportHeight <= 0 ? RootHost.ActualHeight : viewportHeight;
        }

        if (viewportWidth <= 0 || viewportHeight <= 0) return;

        var imageSize = GetImageSize(vm);
        if (imageSize is null) return;

        var cursorX = viewportWidth * anchor.ViewportRatioX;
        var cursorY = viewportHeight * anchor.ViewportRatioY;

        var targetX = anchor.ImageX * currentZoom - cursorX;
        var targetY = anchor.ImageY * currentZoom - cursorY;

        double maxX;
        double maxY;

        if (ZoomScrollViewer is not null && ZoomScrollViewer.ExtentWidth > 0 && ZoomScrollViewer.ExtentHeight > 0)
        {
            maxX = Math.Max(0, ZoomScrollViewer.ExtentWidth - viewportWidth);
            maxY = Math.Max(0, ZoomScrollViewer.ExtentHeight - viewportHeight);
        }
        else
        {
            maxX = Math.Max(0, imageSize.Value.Width * currentZoom - viewportWidth);
            maxY = Math.Max(0, imageSize.Value.Height * currentZoom - viewportHeight);
        }

        if (!double.IsFinite(maxX)) maxX = 0;
        if (!double.IsFinite(maxY)) maxY = 0;

        targetX = Math.Clamp(targetX, 0, maxX);
        targetY = Math.Clamp(targetY, 0, maxY);

        if (ZoomScrollViewer is not null)
        {
            ZoomScrollViewer.ScrollToHorizontalOffset(targetX);
            ZoomScrollViewer.ScrollToVerticalOffset(targetY);
        }

        _isUpdatingFromView = true;
        vm.UpdateScrollOffsets(targetX, targetY);
        _isUpdatingFromView = false;
    }

    private static double ClampRatio(double value)
    {
        if (!double.IsFinite(value)) return 0.5;
        return Math.Clamp(value, 0, 1);
    }

    private void ClearZoomAnchor()
    {
        _activeZoomAnchor = null;
    }

    private double CalculateFitZoomLevel(ImageTabViewModel vm)
    {
        var fitInfo = GetFitDisplayInfo(vm);
        if (fitInfo is null) return vm.ZoomLevel;
        return Math.Clamp(fitInfo.Value.Scale, ZoomMinLevel, ZoomMaxLevel);
    }

    private static double GetZoomStepFactor(ImageTabViewModel vm)
    {
        var percent = Math.Clamp(vm.ZoomStepPercent, 1, 100);
        return 1.0 + (percent / 100.0);
    }

    private static double ToDeviceIndependentLength(int pixels, double dpi)
    {
        var dpiValue = dpi <= 0 ? 96.0 : dpi;
        return pixels * (96.0 / dpiValue);
    }

    private Size? GetImageSize(ImageTabViewModel vm)
    {
        if (vm.Image is not BitmapSource bitmap) return null;
        var width = ToDeviceIndependentLength(bitmap.PixelWidth, bitmap.DpiX);
        var height = ToDeviceIndependentLength(bitmap.PixelHeight, bitmap.DpiY);
        if (width <= 0 || height <= 0) return null;
        return new Size(width, height);
    }

    private FitDisplayInfo? GetFitDisplayInfo(ImageTabViewModel vm)
    {
        if (FitViewbox is null || RootHost is null) return null;
        var imageSize = GetImageSize(vm);
        if (imageSize is null) return null;

        var availableWidth = FitViewbox.ActualWidth > 0 ? FitViewbox.ActualWidth : RootHost.ActualWidth;
        var availableHeight = FitViewbox.ActualHeight > 0 ? FitViewbox.ActualHeight : RootHost.ActualHeight;

        if (availableWidth <= 0 || availableHeight <= 0) return null;

        var widthScale = availableWidth / imageSize.Value.Width;
        var heightScale = availableHeight / imageSize.Value.Height;
        var scale = Math.Min(widthScale, heightScale);
        if (!double.IsFinite(scale) || scale <= 0) return null;

        var displayWidth = imageSize.Value.Width * scale;
        var displayHeight = imageSize.Value.Height * scale;
        var offsetX = (availableWidth - displayWidth) / 2;
        var offsetY = (availableHeight - displayHeight) / 2;

        return new FitDisplayInfo(scale, offsetX, offsetY, displayWidth, displayHeight);
    }

    private void StartZoomAnimation(ImageTabViewModel vm, double targetZoom)
    {
        var savedAnchor = _activeZoomAnchor;
        StopZoomAnimation();
        _activeZoomAnchor = savedAnchor;

        var animation = new DoubleAnimation
        {
            From = vm.ZoomLevel,
            To = targetZoom,
            Duration = ZoomAnimationDuration,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };

        _zoomAnimationFrom = animation.From ?? vm.ZoomLevel;
        _zoomAnimationTo = animation.To ?? targetZoom;
        _activeZoomAnimation = animation;
        _activeZoomClock = animation.CreateClock();

        if (_activeZoomClock is null)
        {
            if (_currentViewModel is not null)
            {
                _currentViewModel.ZoomLevel = targetZoom;
                ApplyZoomAnchor(_currentViewModel, targetZoom);
            }

            ClearZoomAnchor();
            return;
        }

        _activeZoomClock.CurrentTimeInvalidated += OnZoomAnimationFrame;
        _activeZoomClock.Completed += OnZoomAnimationCompleted;
        _activeZoomClock.Controller?.Begin();
    }

    private void OnZoomAnimationFrame(object? sender, EventArgs e)
    {
        if (_activeZoomClock is null || _activeZoomAnimation is null) return;
        if (_currentViewModel is null) return;
        if (_activeZoomClock.CurrentProgress is null) return;

        var value = _activeZoomAnimation.GetCurrentValue(_zoomAnimationFrom, _zoomAnimationTo, _activeZoomClock);
        _currentViewModel.ZoomLevel = value;
        ApplyZoomAnchor(_currentViewModel, value);
    }

    private void OnZoomAnimationCompleted(object? sender, EventArgs e)
    {
        if (_currentViewModel is not null)
        {
            _currentViewModel.ZoomLevel = _zoomAnimationTo;
            ApplyZoomAnchor(_currentViewModel, _zoomAnimationTo);
        }

        StopZoomAnimation();
    }

    private void StopZoomAnimation()
    {
        if (_activeZoomClock is not null)
        {
            _activeZoomClock.CurrentTimeInvalidated -= OnZoomAnimationFrame;
            _activeZoomClock.Completed -= OnZoomAnimationCompleted;
            _activeZoomClock.Controller?.Stop();
            _activeZoomClock = null;
        }

        _activeZoomAnimation = null;
        ClearZoomAnchor();
    }
}
