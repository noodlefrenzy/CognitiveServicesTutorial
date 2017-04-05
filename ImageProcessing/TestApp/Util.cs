//using Microsoft.ProjectOxford.Common;
//using ServiceHelpers;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using System.Threading;
//using System.Threading.Tasks;
//using Windows.Devices.Enumeration;
//using Windows.Graphics.Display;
//using Windows.Graphics.Imaging;
//using Windows.Networking.BackgroundTransfer;
//using Windows.Storage;
//using Windows.Storage.Streams;
//using Windows.UI;
//using Windows.UI.Popups;
//using Windows.UI.Xaml.Media;
//using Windows.UI.Xaml.Media.Imaging;

//namespace TestApp
//{
//    class Util
//	{
//		async public static Task<ImageSource> GetCroppedBitmapAsync(IRandomAccessStream stream, Rectangle rectangle)
//		{
//			var pixels = await GetCroppedPixelsAsync(stream, rectangle);

//			// Stream the bytes into a WriteableBitmap 
//			WriteableBitmap cropBmp = new WriteableBitmap(rectangle.Width, rectangle.Height);
//			cropBmp.FromByteArray(pixels);

//			return cropBmp;
//		}

//		async private static Task CropBitmapAsync(Stream localFileStream, Rectangle rectangle, StorageFile resultFile)
//		{
//			//Get pixels of the crop region
//			var pixels = await GetCroppedPixelsAsync(localFileStream.AsRandomAccessStream(), rectangle);

//			// Save result to new image
//			using (Stream resultStream = await resultFile.OpenStreamForWriteAsync())
//			{
//				IRandomAccessStream randomAccessStream = resultStream.AsRandomAccessStream();
//				BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, randomAccessStream);

//				encoder.SetPixelData(BitmapPixelFormat.Bgra8,
//										BitmapAlphaMode.Ignore,
//										(uint)rectangle.Width, (uint)rectangle.Height,
//										DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi, pixels);

//				await encoder.FlushAsync();
//			}
//		}

//		async public static Task CropBitmapAsync(Func<Task<Stream>> localFile, Microsoft.ProjectOxford.Common.Rectangle rectangle, StorageFile resultFile)
//		{
//			await CropBitmapAsync(await localFile(), rectangle, resultFile);
//		}

//		async private static Task<byte[]> GetCroppedPixelsAsync(IRandomAccessStream stream, Microsoft.ProjectOxford.Common.Rectangle rectangle)
//		{
//			// Create a decoder from the stream. With the decoder, we can get  
//			// the properties of the image. 
//			BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

//			// Create cropping BitmapTransform and define the bounds. 
//			BitmapTransform transform = new BitmapTransform();
//			BitmapBounds bounds = new BitmapBounds();
//			bounds.X = (uint)rectangle.Left;
//			bounds.Y = (uint)rectangle.Top;
//			bounds.Height = (uint)rectangle.Height;
//			bounds.Width = (uint)rectangle.Width;
//			transform.Bounds = bounds;

//			// Get the cropped pixels within the bounds of transform. 
//			PixelDataProvider pix = await decoder.GetPixelDataAsync(
//				BitmapPixelFormat.Bgra8,
//				BitmapAlphaMode.Straight,
//				transform,
//				ExifOrientationMode.IgnoreExifOrientation,
//				ColorManagementMode.ColorManageToSRgb);

//			return pix.DetachPixelData();
//		}

//		internal static async Task<byte[]> GetPixelBytesFromSoftwareBitmapAsync(SoftwareBitmap softwareBitmap)
//		{
//			using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
//			{
//				BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
//				encoder.SetSoftwareBitmap(softwareBitmap);
//				await encoder.FlushAsync();

//				// Read the pixel bytes from the memory stream
//				using (var reader = new DataReader(stream.GetInputStreamAt(0)))
//				{
//					var bytes = new byte[stream.Size];
//					await reader.LoadAsync((uint)stream.Size);
//					reader.ReadBytes(bytes);
//					return bytes;
//				}
//			}
//		}

//        internal static async Task<Stream> ResizePhoto(Stream photo, int height)
//        {
//            WriteableBitmap wb = new WriteableBitmap(1, 1);
//            wb = await wb.FromStream(photo.AsRandomAccessStream());
//            if (wb.PixelHeight <= height)
//            {
//                // no need to resize
//                return photo;
//            }

//            wb = wb.Resize((int)(((double)wb.PixelWidth / wb.PixelHeight) * height), height, WriteableBitmapExtensions.Interpolation.Bilinear);

//            InMemoryRandomAccessStream result = new InMemoryRandomAccessStream();
//            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, result);

//            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
//                                    BitmapAlphaMode.Ignore,
//                                    (uint)wb.PixelWidth, (uint)wb.PixelHeight,
//                                    DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi, wb.PixelBuffer.ToArray());

//            await encoder.FlushAsync();

//            return result.AsStream();
//        }
//    }
//}
