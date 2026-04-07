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
    }

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DuplicateItemViewModel vm)
            vm.IsMarkedForDeletion = !vm.IsMarkedForDeletion;
    }
}
