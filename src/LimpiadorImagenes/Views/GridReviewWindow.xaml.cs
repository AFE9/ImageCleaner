using System.Windows;
using System.Windows.Input;
using LimpiadorImagenes.ViewModels;

namespace LimpiadorImagenes.Views;

public partial class GridReviewWindow : Window
{
    private readonly GridReviewViewModel _vm;
    private readonly MainViewModel _mainVm;

    public GridReviewWindow(GridReviewViewModel vm, MainViewModel mainVm)
    {
        InitializeComponent();
        _vm = vm;
        _mainVm = mainVm;
        DataContext = vm;

        vm.OnMarkingComplete += () => Dispatcher.Invoke(Close);
    }

    private void GridItem_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is GridItemViewModel item)
            item.IsSelected = !item.IsSelected;
    }
}
