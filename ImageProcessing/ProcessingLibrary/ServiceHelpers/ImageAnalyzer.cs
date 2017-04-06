using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public class ImageAnalyzer
    {
        private static FaceAttributeType[] DefaultFaceAttributeTypes = new FaceAttributeType[] { FaceAttributeType.Age, FaceAttributeType.Gender, FaceAttributeType.HeadPose };

        public Func<Task<Stream>> GetImageStreamCallback { get; set; }

        public IEnumerable<Face> DetectedFaces { get; set; }

        public IEnumerable<Emotion> DetectedEmotion { get; set; }

        public IEnumerable<SimilarFaceMatch> SimilarFaceMatches { get; set; }

        public Microsoft.ProjectOxford.Vision.Contract.AnalysisResult AnalysisResult { get; set; }

        public bool ShowDialogOnFaceApiErrors { get; set; } = false;

        public ImageAnalyzer(Func<Task<Stream>> getStreamCallback)
        {
            this.GetImageStreamCallback = getStreamCallback;
        }

        public async Task DetectFacesAsync(bool detectFaceAttributes = false, bool detectFaceLandmarks = false)
        {
            try
            {
                this.DetectedFaces = await FaceServiceHelper.DetectAsync(
                    this.GetImageStreamCallback, 
                    returnFaceId: true, 
                    returnFaceLandmarks: detectFaceLandmarks,
                    returnFaceAttributes: detectFaceAttributes ? DefaultFaceAttributeTypes : null);
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Face API DetectAsync error");

                this.DetectedFaces = Enumerable.Empty<Face>();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Face API failed.");
                }
            }
        }

        public async Task DetectEmotionAsync()
        {
            try
            {
                this.DetectedEmotion = await EmotionServiceHelper.RecognizeAsync(this.GetImageStreamCallback);
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Emotion API RecognizeAsync error");

                this.DetectedEmotion = Enumerable.Empty<Emotion>();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Emotion API failed.");
                }
            }
        }

        public async Task DescribeAsync()
        {
            try
            {
                this.AnalysisResult = await VisionServiceHelper.DescribeAsync(this.GetImageStreamCallback);
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Vision API DescribeAsync error");

                this.AnalysisResult = new Microsoft.ProjectOxford.Vision.Contract.AnalysisResult();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Vision API failed.");
                }
            }
        }

        public async Task AnalyzeImageAsync()
        {
            try
            {
                this.AnalysisResult = await VisionServiceHelper.AnalyzeImageAsync(this.GetImageStreamCallback, new VisualFeature[] { VisualFeature.Tags /*, VisualFeature.Description */});
            }
            catch (Exception e)
            {
                ErrorTrackingHelper.TrackException(e, "Vision API AnalyzeImageAsync error");

                this.AnalysisResult = new Microsoft.ProjectOxford.Vision.Contract.AnalysisResult();

                if (this.ShowDialogOnFaceApiErrors)
                {
                    await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Vision API failed.");
                }
            }
        }

        public async Task FindSimilarPersistedFacesAsync()
        {
            this.SimilarFaceMatches = Enumerable.Empty<SimilarFaceMatch>();

            if (this.DetectedFaces == null || !this.DetectedFaces.Any())
            {
                return;
            }

            List<SimilarFaceMatch> result = new List<SimilarFaceMatch>();

            foreach (Face detectedFace in this.DetectedFaces)
            {
                try
                {
                    SimilarPersistedFace similarPersistedFace = await FaceListManager.FindSimilarPersistedFaceAsync(this.GetImageStreamCallback, detectedFace.FaceId, detectedFace);
                    if (similarPersistedFace != null)
                    {
                        result.Add(new SimilarFaceMatch { Face = detectedFace, SimilarPersistedFace = similarPersistedFace });
                    }
                }
                catch (Exception e)
                {
                    ErrorTrackingHelper.TrackException(e, "FaceListManager.FindSimilarPersistedFaceAsync error");

                    if (this.ShowDialogOnFaceApiErrors)
                    {
                        await ErrorTrackingHelper.GenericApiCallExceptionHandler(e, "Failure finding similar faces");
                    }
                }
            }

            this.SimilarFaceMatches = result;
        }
    }

    public class SimilarFaceMatch
    {
        public Face Face
        {
            get; set;
        }

        public SimilarPersistedFace SimilarPersistedFace
        {
            get; set;
        }
    }
}
