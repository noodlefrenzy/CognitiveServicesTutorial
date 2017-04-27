namespace PictureBot.Models
{
    /// <summary>
    /// This class is not being used anywhere in the solution.
    /// TODO: remove it or leave it so people can understand the 
    /// data better?  
    /// </summary>
    public class Image
    {
        public class Rootobject
        {
            public string id { get; set; }
            public string BlobUri { get; set; }
            public string LocalFilePath { get; set; }
            public string FileName { get; set; }
            public string Caption { get; set; }
            public string[] Tags { get; set; }
            public int NumFaces { get; set; }
            public Face[] Faces { get; set; }
        }

        public class Face
        {
            public string UniqueFaceId { get; set; }
            public Facerectangle FaceRectangle { get; set; }
            public string TopEmotion { get; set; }
            public string Gender { get; set; }
            public float Age { get; set; }
        }

        public class Facerectangle
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int Left { get; set; }
            public int Top { get; set; }
        }

    }
}