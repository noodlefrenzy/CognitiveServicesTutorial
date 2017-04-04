using ImageProcessingLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace TestApp
{
    public class ImageInsightsViewModel
    {
        public ImageInsights Insights { get; set; }
        public ImageSource ImageSource { get; set; }

        public ImageInsightsViewModel(ImageInsights insights, Stream stream)
        {
            this.Insights = insights;

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.SetSource(stream.AsRandomAccessStream());
            this.ImageSource = bitmapImage;
        }
    }
}
