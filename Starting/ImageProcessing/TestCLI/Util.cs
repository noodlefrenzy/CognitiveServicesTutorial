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
    /// <summary>
    /// ImageMetadata stores the image data from Cognitive Services into DocumentDB.
    /// </summary>
    public class ImageMetadata
    {
        /// <summary>
        /// Build from an image path, storing the full local path, but using the filename as ID.
        /// </summary>
        /// <param name="imageFilePath">Local file path.</param>
        public ImageMetadata(string imageFilePath)
        {
            this.LocalFilePath = imageFilePath;
            this.FileName = Path.GetFileName(imageFilePath);
            this.Id = this.FileName; // TODO: Worry about collisions, but ID can't handle slashes.
        }

        /// <summary>
        /// Public parameterless constructor for serialization-friendliness.
        /// </summary>
        public ImageMetadata()
        {
            
        }

        /// <summary>
        /// Store the ImageInsights into the metadata - pulls out tags and caption, stores number of faces and face details.
        /// </summary>
        /// <param name="insights"></param>
        public void AddInsights(ImageInsights insights)
        {
            // TODO - Implement
            // Examine ImageInsights and the properties below. Feel free to alter what you store here as you see fit.
            // However, remember that alterations to the schema extend all the way through to the Azure Search Index,
            //  so you'll need to tune your Bot Framework code as well to ensure your Azure Search queries function
            //  as intended.
            throw new NotImplementedException();
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public Uri BlobUri { get; set; }

        public string LocalFilePath { get; set; }

        public string FileName { get; set; }

        public string Caption { get; set; }

        public string[] Tags { get; set; }

        public int NumFaces { get; set; }

        public FaceInsights[] Faces { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    static class Util
    {
        /// <summary>
        /// Simple resize method for use when we're trying to run the cognitive services against large images. 
        /// We resize downward to avoid too much data over the wire.
        /// </summary>
        /// <param name="imageFile">Image file to resize.</param>
        /// <param name="maxDim">Maximum height/width - will retain aspect ratio.</param>
        /// <returns>Revised width/height and resized image filename.</returns>
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

                    return Tuple.Create(Tuple.Create((double)origImg.Width / width, (double)origImg.Height / height), resizedImageFile);
                }
                else
                {
                    // No need to resize
                    return Tuple.Create((Tuple<double,double>)null, imageFile);
                }
            }
        }

        /// <summary>
        /// If we resize the image, we should resize the face rectangles in our insights appropriately.
        /// </summary>
        /// <param name="insights"></param>
        /// <param name="resizeTransform"></param>
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
