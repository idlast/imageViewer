using System.Windows.Controls;
using System.Windows.Input;
using ImgViewer.ViewModels;

namespace ImgViewer.Views;

public partial class ImageTabView : UserControl
{
    public ImageTabView()
    {
        InitializeComponent();
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        if (DataContext is not ImageTabViewModel vm) return;

        if (e.Delta > 0)
        {
            vm.ZoomIn();
        }
        else
        {
            vm.ZoomOut();
        }

        e.Handled = true;
    }
}
