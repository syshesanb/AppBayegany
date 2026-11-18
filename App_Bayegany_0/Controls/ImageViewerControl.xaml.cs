using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BayeganyApp.Controls
{
    public partial class ImageViewerControl : UserControl
    {
        private Point _lastDragPoint;
        private bool _isDragging = false;

        public event EventHandler? CropRequested;
        public event EventHandler? RotateRequested;

        public BitmapSource? CurrentImageSource => MainImage.Source as BitmapSource;
        public double CurrentRotationAngle => RotateTrans.Angle;

        public ImageViewerControl()
        {
            InitializeComponent();
        }

        private void BtnCrop_Click(object sender, RoutedEventArgs e)
        {
            CropRequested?.Invoke(this, EventArgs.Empty);
        }

        public void SetImage(BitmapSource? source)
        {
            MainImage.Source = source;
            ResetView();
        }

        public void ResetView()
        {
            ScaleTrans.ScaleX = 1.0;
            ScaleTrans.ScaleY = 1.0;
            RotateTrans.Angle = 0;
            ImgScrollViewer.ScrollToHorizontalOffset(0);
            ImgScrollViewer.ScrollToVerticalOffset(0);
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e) => Zoom(1.2);

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e) => Zoom(0.8);

        private void Zoom(double factor)
        {
            double newScaleX = ScaleTrans.ScaleX * factor;
            double newScaleY = ScaleTrans.ScaleY * factor;

            if (newScaleX >= 0.1 && newScaleX <= 10.0)
            {
                ScaleTrans.ScaleX = newScaleX;
                ScaleTrans.ScaleY = newScaleY;
            }
        }

        private void BtnRotate_Click(object sender, RoutedEventArgs e)
        {
            if (RotateRequested != null)
            {
                RotateRequested.Invoke(this, EventArgs.Empty);
            }
            else
            {
                RotateTrans.Angle = (RotateTrans.Angle + 90) % 360;
            }
        }

        private void BtnFit_Click(object sender, RoutedEventArgs e)
        {
            if (MainImage.Source is BitmapSource bmp && ImgScrollViewer.ActualWidth > 0 && ImgScrollViewer.ActualHeight > 0)
            {
                double wRatio = (ImgScrollViewer.ActualWidth - 40) / bmp.PixelWidth;
                double hRatio = (ImgScrollViewer.ActualHeight - 40) / bmp.PixelHeight;
                double fitScale = Math.Min(wRatio, hRatio);

                if (fitScale > 0)
                {
                    ScaleTrans.ScaleX = fitScale;
                    ScaleTrans.ScaleY = fitScale;
                }
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ScaleTrans.ScaleX = 1.0;
            ScaleTrans.ScaleY = 1.0;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                if (e.Delta > 0) Zoom(1.15);
                else Zoom(0.85);
            }
        }

        private void ScrollViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _lastDragPoint = e.GetPosition(ImgScrollViewer);
                ImgScrollViewer.CaptureMouse();
            }
        }

        private void ScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPoint = e.GetPosition(ImgScrollViewer);
                double deltaX = currentPoint.X - _lastDragPoint.X;
                double deltaY = currentPoint.Y - _lastDragPoint.Y;

                ImgScrollViewer.ScrollToHorizontalOffset(ImgScrollViewer.HorizontalOffset - deltaX);
                ImgScrollViewer.ScrollToVerticalOffset(ImgScrollViewer.VerticalOffset - deltaY);

                _lastDragPoint = currentPoint;
            }
        }

        private void ScrollViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ImgScrollViewer.ReleaseMouseCapture();
            }
        }
    }
}
