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
    public class TagFilterViewModel
    {
        public bool IsChecked { get; set; }
        public string Tag { get; set; }

        public TagFilterViewModel(string tag)
        {
            this.Tag = tag;
        }
    }
}
