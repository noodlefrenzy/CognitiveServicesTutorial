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
    public class FaceFilterViewModel
    {
        public bool IsChecked { get; set; }
        public Guid FaceId { get; set; }
        public ImageSource ImageSource { get; set; }

        public FaceFilterViewModel(Guid faceId, ImageSource croppedFace)
        {
            this.FaceId = faceId;
            this.ImageSource = croppedFace;
        }
    }
}
