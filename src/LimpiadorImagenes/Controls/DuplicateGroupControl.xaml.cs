using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LimpiadorImagenes.ViewModels;

namespace LimpiadorImagenes.Controls;

public partial class DuplicateGroupControl : UserControl
{
    public DuplicateGroupControl()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        Focusable = true;
    }

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DuplicateItemViewModel vm) return;

        if (e.ClickCount == 2)
        {
            vm.IsMarkedForDeletion = !vm.IsMarkedForDeletion; // undo the toggle from first click
            if (DataContext is DuplicateGroupViewModel group)
                _ = group.ShowPreviewAsync(vm);
            e.Handled = true;
        }
        else
        {
            vm.IsMarkedForDeletion = !vm.IsMarkedForDeletion;
        }
    }

    private void Lightbox_Close(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DuplicateGroupViewModel group)
            group.ClosePreview();
        e.Handled = true;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is DuplicateGroupViewModel { PreviewImage: not null } group)
        {
            group.ClosePreview();
            e.Handled = true;
        }
    }
}
