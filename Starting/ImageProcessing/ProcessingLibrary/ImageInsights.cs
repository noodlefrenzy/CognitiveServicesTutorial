using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessingLibrary
{
    public class ImageInsights
    {
        public string ImageId { get; set; }
        public FaceInsights[] FaceInsights { get; set; }
        public VisionInsights VisionInsights { get; set; }
    }
}
