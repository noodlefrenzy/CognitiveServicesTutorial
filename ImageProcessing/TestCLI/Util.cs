using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageProcessingLibrary;
using Newtonsoft.Json;

namespace TestCLI
{
    public class ImageMetadata
    {
        public ImageMetadata(string imageFilePath)
        {
            this.LocalFilePath = imageFilePath;
            this.FileName = Path.GetFileName(imageFilePath);
            this.Id = this.FileName; // TODO: Worry about collisions, but ID can't handle slashes.
        }

        public ImageMetadata()
        {
            
        }

        public void AddInsights(ImageInsights insights)
        {
            this.Caption = insights.VisionInsights?.Caption;
            this.Tags = insights.VisionInsights?.Tags;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public Uri BlobUri { get; set; }

        public string LocalFilePath { get; set; }

        public string FileName { get; set; }

        public string Caption { get; set; }

        public string[] Tags { get; set; }

        public FaceInsights[] Faces { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    static class Util
    {
        public static Tuple<Tuple<double, double>, string> ResizeIfRequired(string imageFile, int maxDim)
        {
            using (var origImg = Image.FromFile(imageFile))
            {
                var width = origImg.Width;
                var height = origImg.Height;
                if (width > maxDim || height > maxDim)
                {
                    double aspect = width / (double) height;
                    if (width > maxDim)
                    {
                        width = maxDim;
                        height = (int) (height / aspect);
                    }
                    if (height > maxDim)
                    {
                        aspect = width / (double) height;
                        height = maxDim;
                        width = (int) (height * aspect);
                    }
                    var resizedImageFile = Path.GetTempFileName();
                    using (var resultingImg = (Image)(new Bitmap(origImg, new Size(width, height))))
                        resultingImg.Save(resizedImageFile, ImageFormat.Png);
                    //            return new Tuple<double, double>((double)originalWidth / wb.PixelWidth, (double)originalHeight / wb.PixelHeight);

                    return Tuple.Create(Tuple.Create((double)origImg.Width / width, (double)origImg.Height / height), resizedImageFile);
                }
                else
                {
                    // No need to resize
                    return Tuple.Create((Tuple<double,double>)null, imageFile);
                }
            }
        }

        public static void AdjustFaceInsightsBasedOnResizing(ImageInsights insights, Tuple<double,double> resizeTransform)
        {
            if (resizeTransform == null) return; // No resize was needed.

            foreach (var faceInsight in insights.FaceInsights)
            {
                faceInsight.FaceRectangle.Left = (int)(faceInsight.FaceRectangle.Left * resizeTransform.Item1);
                faceInsight.FaceRectangle.Top = (int)(faceInsight.FaceRectangle.Top * resizeTransform.Item2);
                faceInsight.FaceRectangle.Width = (int)(faceInsight.FaceRectangle.Width * resizeTransform.Item1);
                faceInsight.FaceRectangle.Height = (int)(faceInsight.FaceRectangle.Height * resizeTransform.Item2);
            }
        }
    }
}
