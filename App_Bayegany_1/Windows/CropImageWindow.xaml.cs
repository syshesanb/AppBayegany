using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BayeganyApp.Utils;

namespace BayeganyApp.Windows
{
    public partial class CropImageWindow : Window
    {
        private BitmapSource _sourceBitmap;
        private bool _isSelecting = false;
        private Point _startPoint;
        private Rect _selectedRect = Rect.Empty;

        public BitmapSource? CroppedBitmapSource { get; private set; }
        public byte[]? CroppedBytes { get; private set; }

        public CropImageWindow(BitmapSource sourceBitmap)
        {
            InitializeComponent();
            _sourceBitmap = sourceBitmap ?? throw new ArgumentNullException(nameof(sourceBitmap));
            SourceImage.Source = _sourceBitmap;
            Loaded += CropImageWindow_Loaded;
        }

        private void CropImageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMasks();
        }

        private void ImageContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateMasks();
        }

        private Rect GetDisplayedImageRect()
        {
            if (_sourceBitmap == null || ImageContainer.ActualWidth <= 0 || ImageContainer.ActualHeight <= 0)
                return Rect.Empty;

            double imgW = _sourceBitmap.PixelWidth;
            double imgH = _sourceBitmap.PixelHeight;
            double contW = ImageContainer.ActualWidth;
            double contH = ImageContainer.ActualHeight;

            double scale = Math.Min(contW / imgW, contH / imgH);
            double displayW = imgW * scale;
            double displayH = imgH * scale;
            double left = (contW - displayW) / 2.0;
            double top = (contH - displayH) / 2.0;

            return new Rect(left, top, displayW, displayH);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Rect dispRect = GetDisplayedImageRect();
                if (dispRect.IsEmpty) return;

                Point pt = e.GetPosition(SelectionCanvas);
                pt.X = Math.Clamp(pt.X, dispRect.Left, dispRect.Right);
                pt.Y = Math.Clamp(pt.Y, dispRect.Top, dispRect.Bottom);

                _startPoint = pt;
                _selectedRect = new Rect(_startPoint, _startPoint);
                _isSelecting = true;
                SelectionCanvas.CaptureMouse();
                UpdateCropVisuals();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                Rect dispRect = GetDisplayedImageRect();
                if (dispRect.IsEmpty) return;

                Point currentPt = e.GetPosition(SelectionCanvas);
                currentPt.X = Math.Clamp(currentPt.X, dispRect.Left, dispRect.Right);
                currentPt.Y = Math.Clamp(currentPt.Y, dispRect.Top, dispRect.Bottom);

                double x = Math.Min(_startPoint.X, currentPt.X);
                double y = Math.Min(_startPoint.Y, currentPt.Y);
                double w = Math.Abs(currentPt.X - _startPoint.X);
                double h = Math.Abs(currentPt.Y - _startPoint.Y);

                _selectedRect = new Rect(x, y, w, h);
                UpdateCropVisuals();
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                SelectionCanvas.ReleaseMouseCapture();

                if (_selectedRect.Width < 8 || _selectedRect.Height < 8)
                {
                    _selectedRect = Rect.Empty;
                }
                UpdateCropVisuals();
            }
        }

        private void UpdateCropVisuals()
        {
            if (_selectedRect.IsEmpty || _selectedRect.Width < 5 || _selectedRect.Height < 5)
            {
                CropBox.Visibility = Visibility.Collapsed;
                TxtCropDimensions.Text = "کادر برش: هنوز انتخابی انجام نشده است";
                UpdateMasks();
                return;
            }

            CropBox.Visibility = Visibility.Visible;
            Canvas.SetLeft(CropBox, _selectedRect.Left);
            Canvas.SetTop(CropBox, _selectedRect.Top);
            CropBox.Width = _selectedRect.Width;
            CropBox.Height = _selectedRect.Height;

            // محاسبه ابعاد واقعی بر حسب پیکسل
            Rect dispRect = GetDisplayedImageRect();
            if (!dispRect.IsEmpty && dispRect.Width > 0 && dispRect.Height > 0)
            {
                double scaleX = _sourceBitmap.PixelWidth / dispRect.Width;
                double scaleY = _sourceBitmap.PixelHeight / dispRect.Height;
                int actualW = (int)(_selectedRect.Width * scaleX);
                int actualH = (int)(_selectedRect.Height * scaleY);
                TxtCropDimensions.Text = $"کادر برش: {actualW} × {actualH} پیکسل";
            }

            UpdateMasks();
        }

        private void UpdateMasks()
        {
            Rect dispRect = GetDisplayedImageRect();
            if (dispRect.IsEmpty)
            {
                MaskTop.Visibility = Visibility.Collapsed;
                MaskBottom.Visibility = Visibility.Collapsed;
                MaskLeft.Visibility = Visibility.Collapsed;
                MaskRight.Visibility = Visibility.Collapsed;
                return;
            }

            double totalW = ImageContainer.ActualWidth;
            double totalH = ImageContainer.ActualHeight;

            if (_selectedRect.IsEmpty || _selectedRect.Width < 5 || _selectedRect.Height < 5)
            {
                MaskTop.Visibility = Visibility.Collapsed;
                MaskBottom.Visibility = Visibility.Collapsed;
                MaskLeft.Visibility = Visibility.Collapsed;
                MaskRight.Visibility = Visibility.Collapsed;
                return;
            }

            MaskTop.Visibility = Visibility.Visible;
            MaskBottom.Visibility = Visibility.Visible;
            MaskLeft.Visibility = Visibility.Visible;
            MaskRight.Visibility = Visibility.Visible;

            // ۱. ماسک بالا
            Canvas.SetLeft(MaskTop, 0);
            Canvas.SetTop(MaskTop, 0);
            MaskTop.Width = totalW;
            MaskTop.Height = Math.Max(0, _selectedRect.Top);

            // ۲. ماسک پایین
            Canvas.SetLeft(MaskBottom, 0);
            Canvas.SetTop(MaskBottom, _selectedRect.Bottom);
            MaskBottom.Width = totalW;
            MaskBottom.Height = Math.Max(0, totalH - _selectedRect.Bottom);

            // ۳. ماسک چپ
            Canvas.SetLeft(MaskLeft, 0);
            Canvas.SetTop(MaskLeft, _selectedRect.Top);
            MaskLeft.Width = Math.Max(0, _selectedRect.Left);
            MaskLeft.Height = _selectedRect.Height;

            // ۴. ماسک راست
            Canvas.SetLeft(MaskRight, _selectedRect.Right);
            Canvas.SetTop(MaskRight, _selectedRect.Top);
            MaskRight.Width = Math.Max(0, totalW - _selectedRect.Right);
            MaskRight.Height = _selectedRect.Height;
        }

        private void BtnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            RotateImage(90);
        }

        private void BtnRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            RotateImage(-90);
        }

        private void RotateImage(double angle)
        {
            _sourceBitmap = ImageHelper.RotateBitmap(_sourceBitmap, angle);
            SourceImage.Source = _sourceBitmap;
            _selectedRect = Rect.Empty;
            UpdateCropVisuals();
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            Rect dispRect = GetDisplayedImageRect();
            if (!dispRect.IsEmpty)
            {
                _selectedRect = dispRect;
                UpdateCropVisuals();
            }
        }

        private void BtnResetSelection_Click(object sender, RoutedEventArgs e)
        {
            _selectedRect = Rect.Empty;
            UpdateCropVisuals();
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            Rect dispRect = GetDisplayedImageRect();
            if (dispRect.IsEmpty || dispRect.Width <= 0 || dispRect.Height <= 0)
            {
                MessageBox.Show("تصویر به درستی بارگذاری نشده است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedRect.IsEmpty || _selectedRect.Width < 15 || _selectedRect.Height < 15)
            {
                MessageBox.Show("لطفاً با کشیدن ماوس روی تصویر، کادر دلخواه خود را جهت برش مشخص کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                double scaleX = _sourceBitmap.PixelWidth / dispRect.Width;
                double scaleY = _sourceBitmap.PixelHeight / dispRect.Height;

                int cropX = (int)Math.Round((_selectedRect.X - dispRect.X) * scaleX);
                int cropY = (int)Math.Round((_selectedRect.Y - dispRect.Y) * scaleY);
                int cropW = (int)Math.Round(_selectedRect.Width * scaleX);
                int cropH = (int)Math.Round(_selectedRect.Height * scaleY);

                cropX = Math.Clamp(cropX, 0, _sourceBitmap.PixelWidth - 1);
                cropY = Math.Clamp(cropY, 0, _sourceBitmap.PixelHeight - 1);
                cropW = Math.Clamp(cropW, 1, _sourceBitmap.PixelWidth - cropX);
                cropH = Math.Clamp(cropH, 1, _sourceBitmap.PixelHeight - cropY);

                if (cropW < 5 || cropH < 5)
                {
                    MessageBox.Show("ابعاد کادر انتخابی نامعتبر است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var rect = new Int32Rect(cropX, cropY, cropW, cropH);
                CroppedBitmapSource = ImageHelper.CropBitmap(_sourceBitmap, rect);
                CroppedBytes = ImageHelper.BitmapSourceToJpegBytes(CroppedBitmapSource);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در برش تصویر: " + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
