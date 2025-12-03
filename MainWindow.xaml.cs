using System.Windows;
using System.Windows.Input;
using ImgViewer.ViewModels;

namespace ImgViewer;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
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
}