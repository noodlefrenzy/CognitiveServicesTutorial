using ImageProcessingLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TestWpfApp
{
    public class ImageInsightsViewModel
    {
        public ImageInsights Insights { get; set; }
        public ImageSource ImageSource { get; set; }

        public ImageInsightsViewModel(ImageInsights insights, ImageSource imageSource)
        {
            this.Insights = insights;
            this.ImageSource = imageSource;
        }
    }
}
