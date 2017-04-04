using Microsoft.ProjectOxford.Face.Contract;
using System;

namespace ImageProcessingLibrary
{
    public class FaceInsights
    {
        public Guid UniqueFaceId { get; set; }
        public FaceRectangle FaceRectangle { get; set; }
        public string TopEmotion { get; set; }
    }
}
