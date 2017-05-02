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
    public class EmotionFilterViewModel
    {
        public bool IsChecked { get; set; }
        public string Emotion { get; set; }

        public EmotionFilterViewModel(string emotion)
        {
            this.Emotion = emotion;
        }
    }
}
