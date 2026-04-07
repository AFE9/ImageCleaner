using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using LimpiadorImagenes.Messages;
using LimpiadorImagenes.ViewModels;

namespace LimpiadorImagenes.Controls;

public partial class ProximityBarControl : UserControl
{
    public ProximityBarControl()
    {
        InitializeComponent();
    }

    private void ThumbItem_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ProximityItemViewModel vm)
            WeakReferenceMessenger.Default.Send(new NavigateToPathMessage(vm.File.FullPath));
    }
}
