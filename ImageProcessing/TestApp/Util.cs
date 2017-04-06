using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace TestApp
{
    class Util
    {
        internal static void ShowToastNotification(string errorMessage)
        {
            ToastTemplateType toastTemplate = ToastTemplateType.ToastText02;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode("Image Processing Test App"));
            toastTextElements[1].AppendChild(toastXml.CreateTextNode(errorMessage));

            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        internal static async Task GenericApiCallExceptionHandler(Exception ex, string errorTitle)
        {
            string errorDetails = GetMessageFromException(ex);

            await new MessageDialog(errorDetails, errorTitle).ShowAsync();
        }

        internal static string GetMessageFromException(Exception ex)
        {
            string errorDetails = ex.Message;

            FaceAPIException faceApiException = ex as FaceAPIException;
            if (faceApiException?.ErrorMessage != null)
            {
                errorDetails = faceApiException.ErrorMessage;
            }

            ClientException commonException = ex as ClientException;
            if (commonException?.Error?.Message != null)
            {
                errorDetails = commonException.Error.Message;
            }

            Microsoft.ProjectOxford.Vision.ClientException visionException = ex as Microsoft.ProjectOxford.Vision.ClientException;
            if (visionException?.Error?.Message != null)
            {
                errorDetails = visionException.Error.Message;
            }

            return errorDetails;
        }

        async public static Task<ImageSource> GetCroppedBitmapAsync(IRandomAccessStream stream, FaceRectangle rectangle)
        {
            var pixels = await GetCroppedPixelsAsync(stream, rectangle);

            // Stream the bytes into a WriteableBitmap 
            WriteableBitmap cropBmp = new WriteableBitmap(rectangle.Width, rectangle.Height);
            cropBmp.FromByteArray(pixels);

            return cropBmp;
        }

        async private static Task<byte[]> GetCroppedPixelsAsync(IRandomAccessStream stream, FaceRectangle rectangle)
        {
            // Create a decoder from the stream. With the decoder, we can get  
            // the properties of the image. 
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            // Create cropping BitmapTransform and define the bounds. 
            BitmapTransform transform = new BitmapTransform();
            BitmapBounds bounds = new BitmapBounds();
            bounds.X = (uint)rectangle.Left;
            bounds.Y = (uint)rectangle.Top;
            bounds.Height = (uint)rectangle.Height;
            bounds.Width = (uint)rectangle.Width;
            transform.Bounds = bounds;

            // Get the cropped pixels within the bounds of transform. 
            PixelDataProvider pix = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            return pix.DetachPixelData();
        }

        internal static async Task<Stream> ResizePhoto(Stream photo, int height)
        {
            WriteableBitmap wb = new WriteableBitmap(1, 1);
            wb = await wb.FromStream(photo.AsRandomAccessStream());
            if (wb.PixelHeight <= height)
            {
                // no need to resize
                return photo;
            }

            wb = wb.Resize((int)(((double)wb.PixelWidth / wb.PixelHeight) * height), height, WriteableBitmapExtensions.Interpolation.Bilinear);

            InMemoryRandomAccessStream result = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, result);

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                    BitmapAlphaMode.Ignore,
                                    (uint)wb.PixelWidth, (uint)wb.PixelHeight,
                                    DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi, wb.PixelBuffer.ToArray());

            await encoder.FlushAsync();

            return result.AsStream();
        }
    }
}
