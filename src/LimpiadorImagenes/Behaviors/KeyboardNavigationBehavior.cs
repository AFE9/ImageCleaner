using System.Windows;
using System.Windows.Input;
using LimpiadorImagenes.Controls;
using LimpiadorImagenes.ViewModels;

namespace LimpiadorImagenes.Behaviors;

/// <summary>
/// Attached behavior that intercepts keyboard shortcuts at the Window level (tunneling PreviewKeyDown).
/// Attach via KeyboardNavigationBehavior.ViewModel attached property on the Window.
/// </summary>
public static class KeyboardNavigationBehavior
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.RegisterAttached(
            "ViewModel",
            typeof(MainViewModel),
            typeof(KeyboardNavigationBehavior),
            new PropertyMetadata(null, OnViewModelChanged));

    public static void SetViewModel(Window target, MainViewModel? value)
        => target.SetValue(ViewModelProperty, value);

    public static MainViewModel? GetViewModel(Window target)
        => (MainViewModel?)target.GetValue(ViewModelProperty);

    private static FileViewerControl? _viewer;

    public static void SetViewer(FileViewerControl viewer) => _viewer = viewer;

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window) return;

        window.PreviewKeyDown -= OnPreviewKeyDown;
        window.PreviewKeyUp   -= OnPreviewKeyUp;

        if (e.NewValue is MainViewModel)
        {
            window.PreviewKeyDown += OnPreviewKeyDown;
            window.PreviewKeyUp   += OnPreviewKeyUp;
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not Window window) return;
        var vm = GetViewModel(window);
        if (vm == null) return;

        // Ignore when a text field is focused
        if (Keyboard.FocusedElement is System.Windows.Controls.TextBox) return;

        switch (e.Key)
        {
            case Key.Right:
            case Key.Space:
                if (!e.IsRepeat)
                    vm.KeepCurrentFileCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Left:
            case Key.Back:
                if (!e.IsRepeat)
                    vm.MarkCurrentForDeletionCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Z:
                _viewer?.SetMagnifier(true);
                e.Handled = true;
                break;

            case Key.U:
                vm.UndoLastMarkCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private static void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Z)
            _viewer?.SetMagnifier(false);
    }
}
