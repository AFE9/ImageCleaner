using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LimpiadorImagenes.ViewModels;

namespace LimpiadorImagenes.Controls;

public partial class FileViewerControl : UserControl
{
    private ViewerViewModel? _vm;

    public FileViewerControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _vm = DataContext as ViewerViewModel;

        if (_vm != null)
        {
            _vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ViewerViewModel.CurrentPreview))
                    OnPreviewChanged();
            };
        }
    }

    private void OnPreviewChanged()
    {
        var preview = _vm?.CurrentPreview;

        // Handle video
        if (preview?.IsVideo == true && preview.VideoPath != null)
        {
            VideoView.Source = new Uri(preview.VideoPath);
            VideoView.Play();
        }
        else
        {
            VideoView.Stop();
            VideoView.Source = null;
        }
    }

    private void VideoView_MediaEnded(object sender, RoutedEventArgs e)
    {
        VideoView.Position = TimeSpan.Zero;
        VideoView.Play();
    }

    // ── Magnifier ──────────────────────────────────────────────────────────

    private void ImageView_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            ActivateMagnifier(e.GetPosition(RootGrid));
    }

    private void ImageView_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_vm != null) _vm.IsMagnifierActive = false;
    }

    private void ImageView_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _vm?.IsMagnifierActive == true)
            UpdateMagnifier(e.GetPosition(RootGrid));
    }

    private void ActivateMagnifier(Point position)
    {
        if (_vm == null || ImageView.Source is not BitmapSource source) return;
        _vm.IsMagnifierActive = true;
        UpdateMagnifier(position);
    }

    private void UpdateMagnifier(Point mousePos)
    {
        if (ImageView.Source is not BitmapSource source) return;

        // Compute what portion of the image is under the cursor
        var imgRect = GetImageRect();
        if (imgRect.Width <= 0 || imgRect.Height <= 0) return;

        double relX = (mousePos.X - imgRect.X) / imgRect.Width;
        double relY = (mousePos.Y - imgRect.Y) / imgRect.Height;

        int pixX = (int)(relX * source.PixelWidth);
        int pixY = (int)(relY * source.PixelHeight);

        int cropSize = 110; // pixels in source image
        int x = Math.Max(0, Math.Min(pixX - cropSize / 2, source.PixelWidth - cropSize));
        int y = Math.Max(0, Math.Min(pixY - cropSize / 2, source.PixelHeight - cropSize));
        int w = Math.Min(cropSize, source.PixelWidth - x);
        int h = Math.Min(cropSize, source.PixelHeight - y);

        if (w <= 0 || h <= 0) return;

        var cropped = new CroppedBitmap(source, new Int32Rect(x, y, w, h));
        cropped.Freeze();
        MagnifierImage.Source = cropped;
        MagnifierImage.Width = w * 2;
        MagnifierImage.Height = h * 2;

        // Position the magnifier lens near cursor (offset so it doesn't cover the spot)
        double lensLeft = mousePos.X + 20;
        double lensTop  = mousePos.Y - 120;
        lensLeft = Math.Max(0, Math.Min(lensLeft, ActualWidth  - MagnifierBorder.Width));
        lensTop  = Math.Max(0, Math.Min(lensTop,  ActualHeight - MagnifierBorder.Height));

        Canvas.SetLeft(MagnifierBorder, lensLeft);
        Canvas.SetTop(MagnifierBorder,  lensTop);
    }

    private Rect GetImageRect()
    {
        if (ImageView.Source is not BitmapSource source) return Rect.Empty;

        double srcW = source.PixelWidth;
        double srcH = source.PixelHeight;
        double ctrlW = ImageView.ActualWidth;
        double ctrlH = ImageView.ActualHeight;

        double scale = Math.Min(ctrlW / srcW, ctrlH / srcH);
        double w = srcW * scale;
        double h = srcH * scale;
        double x = (ctrlW - w) / 2;
        double y = (ctrlH - h) / 2;

        // Translate to RootGrid coordinates
        var transform = ImageView.TransformToAncestor(RootGrid);
        var origin = transform.Transform(new Point(x, y));
        return new Rect(origin, new Size(w, h));
    }

    // Keyboard magnifier toggle (called by behavior)
    public void SetMagnifier(bool active)
    {
        if (_vm != null) _vm.IsMagnifierActive = active;
    }
}
