using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BayeganyApp.Utils
{
    public static class ImageHelper
    {
        public static byte[] CompressImageFromFile(string filePath, int maxDimension = 1600, int quality = 80)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return Array.Empty<byte>();

            byte[] fileBytes = File.ReadAllBytes(filePath);
            return CompressImageBytes(fileBytes, maxDimension, quality);
        }

        public static byte[] CompressImageBytes(byte[] rawBytes, int maxDimension = 1600, int quality = 80)
        {
            if (rawBytes == null || rawBytes.Length == 0)
                return Array.Empty<byte>();

            try
            {
                using var memIn = new MemoryStream(rawBytes);
                var decoder = BitmapDecoder.Create(memIn, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count == 0)
                    return rawBytes;

                var frame = decoder.Frames[0];
                int origWidth = frame.PixelWidth;
                int origHeight = frame.PixelHeight;

                BitmapSource sourceToEncode = frame;

                if (origWidth > maxDimension || origHeight > maxDimension)
                {
                    double scale = Math.Min((double)maxDimension / origWidth, (double)maxDimension / origHeight);
                    int targetWidth = (int)(origWidth * scale);
                    int targetHeight = (int)(origHeight * scale);

                    var transformed = new TransformedBitmap(frame, new System.Windows.Media.ScaleTransform(scale, scale));
                    sourceToEncode = transformed;
                }

                var encoder = new JpegBitmapEncoder { QualityLevel = quality };
                encoder.Frames.Add(BitmapFrame.Create(sourceToEncode));

                using var memOut = new MemoryStream();
                encoder.Save(memOut);
                return memOut.ToArray();
            }
            catch
            {
                return rawBytes;
            }
        }

        public static byte[] GenerateThumbnail(byte[] imageBytes, int maxDim = 220, int quality = 65)
        {
            return CompressImageBytes(imageBytes, maxDim, quality);
        }

        public static BitmapSource? BytesToBitmapSource(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            try
            {
                using var stream = new MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // جهت کارکرد ایمن بین تردها
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static byte[] BitmapSourceToJpegBytes(BitmapSource bitmap, int quality = 85)
        {
            try
            {
                var encoder = new JpegBitmapEncoder { QualityLevel = quality };
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                return ms.ToArray();
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        public static BitmapSource CropBitmap(BitmapSource source, Int32Rect rect)
        {
            var cropped = new CroppedBitmap(source, rect);
            cropped.Freeze();
            return cropped;
        }

        public static BitmapSource RotateBitmap(BitmapSource source, double angle)
        {
            angle = (angle % 360 + 360) % 360;
            if (angle == 0) return source;

            var transformed = new TransformedBitmap(source, new System.Windows.Media.RotateTransform(angle));
            transformed.Freeze();
            return transformed;
        }
    }
}
